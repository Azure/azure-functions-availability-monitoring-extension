using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
#pragma warning disable CS0618 // Type or member is obsolete (Filter-related types are obsolete, but we want to use them)
    internal class FunctionInvocationManagementFilter : IFunctionInvocationFilter
#pragma warning restore CS0618 // Type or member is obsolete (Filter-related types are obsolete, but we want to use them)
    {
        private static string FormatActivityName(string testDisplayName, string locationDisplayName)
        {
            return String.Format("{0} / {1}", testDisplayName, locationDisplayName);
        }

        private static string FormatActivitySpanId(Activity activity)
        {
            if (activity == null)
            {
                return null;
            }

            return activity.SpanId.ToHexString();
        }

        private static bool IsUserCodeTimeout(Exception error, CancellationToken cancelControl)
        {
            bool IsUserCodeTimeout = (cancelControl == (error as TaskCanceledException)?.CancellationToken);
            return IsUserCodeTimeout;
        }


        private readonly TelemetryClient _telemetryClient;

        public FunctionInvocationManagementFilter(TelemetryClient telemetryClient)
        {
            Validate.NotNull(telemetryClient, nameof(telemetryClient));
            _telemetryClient = telemetryClient;
        }

#pragma warning disable CS0618 // Type or member is obsolete (Filter-related types are obsolete, but we want to use them)
        public Task OnExecutingAsync(FunctionExecutingContext executingContext, CancellationToken cancelControl)
#pragma warning restore CS0618 // Type or member is obsolete (Filter-related types are obsolete, but we want to use them)
        {
            Validate.NotNull(executingContext, nameof(executingContext));

            // Check that the functionInstanceId is registered.
            // If not, this function does not have a parameter marked with AvailabilityTestAttribute. In that case there is nothing to do:
            bool isAvailabilityTest = FunctionInvocationStateCache.SingeltonInstance.TryStartFunctionInvocation(
                                                                                            executingContext.FunctionInstanceId,
                                                                                            out FunctionInvocationState invocationState);
            if (! isAvailabilityTest)
            {
                return Task.CompletedTask;
            }

            // Start activity:
            string activityName = FormatActivityName(invocationState.AvailabilityTestInfo.TestDisplayName, invocationState.AvailabilityTestInfo.LocationDisplayName);
            invocationState.ActivitySpan = new Activity(activityName).Start();

            invocationState.AvailabilityTestInfo.AvailabilityResult.Id = FormatActivitySpanId(invocationState.ActivitySpan);

            // Start the timer:
            DateTimeOffset startTime = DateTimeOffset.Now;
            invocationState.AvailabilityTestInfo.SetStartTime(startTime);

            // Done:
            return Task.CompletedTask;
        }

#pragma warning disable CS0618 // Type or member is obsolete (Filter-related types are obsolete, but we want to use them)
        public Task OnExecutedAsync(FunctionExecutedContext executedContext, CancellationToken cancelControl)
#pragma warning restore CS0618 // Type or member is obsolete (Filter-related types are obsolete, but we want to use them)
        {
            Validate.NotNull(executedContext, nameof(executedContext));

            // Check that the functionInstanceId is registered.
            // If not, this function does not have a parameter marked with AvailabilityTestAttribute. In that case there is nothing to do:
            bool isAvailabilityTest = FunctionInvocationStateCache.SingeltonInstance.TryCompleteFunctionInvocation(
                                                                                           executedContext.FunctionInstanceId,
                                                                                           out FunctionInvocationState invocationState);
            if (!isAvailabilityTest)
            {
                return Task.CompletedTask;
            }

            // Measure user time (plus the minimal runtime overhead within the bracket of this binding:
            DateTimeOffset endTime = DateTimeOffset.Now;

            // Stop activity & handle related errors (this will throw if activity is null):
            string activitySpadId;
            try
            {
                activitySpadId = FormatActivitySpanId(invocationState.ActivitySpan);
                invocationState.ActivitySpan.Stop();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error while stopping {nameof(invocationState.ActivitySpan)}.", ex);
            }

            // Get Function result (failed or not):
            bool errorOcurred = ! executedContext.FunctionResult.Succeeded;
            Exception error = errorOcurred 
                                    ? executedContext.FunctionResult.Exception
                                    : null;

            // Find the parameter in the list of parameters:
            AvailabilityTestInvocation availabilityTestInfo = FindAvailabilityTestParameter(invocationState, executedContext.Arguments, errorOcurred, activitySpadId);

            if (errorOcurred)
            {
                // If the user code completed with an error or a timeout, then the Test resuls is always "fail":
                availabilityTestInfo.AvailabilityResult.Success = false;

                // Track the exception:
                IDictionary<string, string> exProps = OutputTelemetryFormat.CreateExceptionCustomPropertiesForError(availabilityTestInfo.AvailabilityResult);

                ITelemetry errorTelemetry = (error == null)
                                                        ? (ITelemetry) new TraceTelemetry(OutputTelemetryFormat.ErrorSetButNotSpecified, SeverityLevel.Error)
                                                        : (ITelemetry) new ExceptionTelemetry(error);
                foreach (KeyValuePair<string, string> prop in exProps)
                {
                    ((ISupportProperties) errorTelemetry).Properties[prop.Key] = prop.Value;
                }

                // @ToDo: How do we make sure that we do not double-track this exception?
                _telemetryClient.Track(errorTelemetry);

                // Add references about the exception we just tracked to the availability result:
                IDictionary<string, string> avResProps = OutputTelemetryFormat.CreateAvailabilityResultCustomPropertiesForError(errorTelemetry as ExceptionTelemetry);
                foreach (KeyValuePair<string, string> prop in avResProps)
                {
                    availabilityTestInfo.AvailabilityResult.Properties[prop.Key] = prop.Value;
                }
            }

            // If user did not initialize Message, initialize it to default value according to the result:
            if (String.IsNullOrEmpty(availabilityTestInfo.AvailabilityResult.Message))
            {
                availabilityTestInfo.AvailabilityResult.Message = errorOcurred
                                                                ? IsUserCodeTimeout(error, cancelControl)
                                                                        ? OutputTelemetryFormat.DefaultResultMessage_Timeout
                                                                        : OutputTelemetryFormat.DefaultResultMessage_Error
                                                                : availabilityTestInfo.AvailabilityResult.Success
                                                                        ? OutputTelemetryFormat.DefaultResultMessage_Pass
                                                                        : OutputTelemetryFormat.DefaultResultMessage_Fail;
            }

            // If user did not initialize Duration, initialize it to default value according to the measurement:
            if (availabilityTestInfo.AvailabilityResult.Duration == TimeSpan.Zero)
            {
                availabilityTestInfo.AvailabilityResult.Duration = endTime - availabilityTestInfo.AvailabilityResult.Timestamp;
            }

            // Send the availability result to the backend:
            OutputTelemetryFormat.RemoveFunctionInstanceIdMarker(availabilityTestInfo.AvailabilityResult);
            _telemetryClient.TrackAvailability(availabilityTestInfo.AvailabilityResult);

            // Make sure everyting we trracked is put on the wire, if case the Function runtime shuts down:
            _telemetryClient.Flush();

            return Task.CompletedTask;
        }
        

        private AvailabilityTestInvocation FindAvailabilityTestParameter(FunctionInvocationState invocationState, IReadOnlyDictionary<string, object> arguments, bool errorOcurred, string activitySpadId)
        {
            // Find the right paraeter based on heuristics.

            // Look at each argument:
            if (arguments != null)
            {
                foreach (object argument in arguments.Values)
                {
                    // Skip null value:
                    if (argument == null)
                    {
                        continue;
                    }

                    {
                        // If this argument is a AvailabilityTestInvocation:
                        var testInfoArgument = argument as AvailabilityTestInvocation;
                        if (testInfoArgument != null)
                        {
                            // Check FunctionInstanceId match:
                            if (testInfoArgument.FunctionInstanceId == invocationState.FunctionInstanceId)
                            {
                                // Ok, this is the right argument. Validate the span ID (may throw) and then return:
                                ValidateactivitySpanId(activitySpadId, testInfoArgument);
                                return testInfoArgument;
                            }
                        }
                    }

                    {
                        // If this argument is a AvailabilityTestInvocation:
                        var availabilityResultArgument = argument as AvailabilityTelemetry;
                        if (availabilityResultArgument != null)
                        {
                            // Check FunctionInstanceId match:
                            if (OutputTelemetryFormat.GetFunctionInstanceId(availabilityResultArgument) == invocationState.FunctionInstanceId)
                            {
                                // Ok, this is the right argument. Convert to AvailabilityTestInvocation:
                                AvailabilityTestInvocation testInfoArgument = Convert.AvailabilityTelemetryToAvailabilityTestInvocation(availabilityResultArgument);

                                //Validate the span ID (may throw) and then return:
                                ValidateactivitySpanId(activitySpadId, testInfoArgument);
                                return testInfoArgument;
                            }
                        }
                    }

                    {
                        // If this argument is a JObject:
                        var jObjectArgument = argument as JObject;
                        if (jObjectArgument != null)
                        {
                            {
                                // Perhaps this jObjectArgument encodes a AvailabilityTestInvocation:
                                AvailabilityTestInvocation testInfoArgument = Convert.JObjectToAvailabilityTestInvocation(jObjectArgument);
                                if (testInfoArgument != null && testInfoArgument.FunctionInstanceId == invocationState.FunctionInstanceId)
                                {
                                    // Ok, this is the right argument. Validate the span ID (may throw) and then return:
                                    ValidateactivitySpanId(activitySpadId, testInfoArgument);
                                    return testInfoArgument;
                                }
                            }

                            { 
                                // Perhaps this jObjectArgument encodes a AvailabilityTelemetry:
                                AvailabilityTelemetry availabilityResultArgument = Convert.JObjectToAvailabilityTelemetry(jObjectArgument);
                                if (availabilityResultArgument != null && OutputTelemetryFormat.GetFunctionInstanceId(availabilityResultArgument) == invocationState.FunctionInstanceId)
                                {
                                    // Ok, this is the right argument. Validate the span ID (may throw) and then return:
                                    // Ok, this is the right argument. Convert to AvailabilityTestInvocation:
                                    AvailabilityTestInvocation testInfoArgument = Convert.AvailabilityTelemetryToAvailabilityTestInvocation(availabilityResultArgument);

                                    //Validate the span ID (may throw) and then return:
                                    ValidateactivitySpanId(activitySpadId, testInfoArgument);
                                    return testInfoArgument;
                                }
                            }
                        }
                    }
                }
            }

            // We did not find anything.
            // If the function failed, we accept it and generate a default failure result based on the state.
            // Otherwise something is badly wrong:

            if (errorOcurred)
            {
                AvailabilityTestInvocation originalValue = invocationState.AvailabilityTestInfo;
                originalValue.AvailabilityResult.Id = activitySpadId;
                return originalValue;
            }

            throw new InvalidOperationException($"Could not find the parameter previusly attributed with {nameof(AvailabilityTestAttribute)}."
                                              + $" It is either not present, has an unexpected type or does not carry the right {nameof(invocationState.FunctionInstanceId)}"
                                              + $" ({invocationState.FormattedFunctionInstanceId})");
        }

        private void ValidateactivitySpanId(string activitySpanId, AvailabilityTestInvocation availabilityTestInfo)
        {
            if (! activitySpanId.Equals(availabilityTestInfo.AvailabilityResult.Id, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"The {nameof(availabilityTestInfo.AvailabilityResult.Id)} of"
                                                  + $" the {nameof(availabilityTestInfo.AvailabilityResult)} does not match the"
                                                  + $" span Id of the activity span associated with this function:"
                                                  + $" {nameof(availabilityTestInfo.AvailabilityResult)}.{nameof(availabilityTestInfo.AvailabilityResult.Id)}"
                                                  + $"=\"{availabilityTestInfo.AvailabilityResult.Id}\";"
                                                  + $" {nameof(activitySpanId)}=\"{activitySpanId}\".");
            }                                            
        }
    }

}

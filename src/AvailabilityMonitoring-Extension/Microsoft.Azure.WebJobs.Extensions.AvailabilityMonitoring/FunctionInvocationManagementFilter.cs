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

            IdentifyManagedParameters(invocationState, executingContext.Arguments);

            // Start activity:
            string activityName = Convert.NotNullOrWord(invocationState.ActivitySpanName);
            invocationState.ActivitySpan = new Activity(activityName).Start();
            string activitySpanId = invocationState.ActivitySpan.SpanId.ToHexString();
            
            // Start the timer:
            DateTimeOffset startTime = DateTimeOffset.Now;

            // Look at every paramater and update it with the activity ID and the start time:
            foreach (FunctionInvocationState.Parameter regRaram in invocationState.Parameters.Values)
            {
                regRaram.AvailabilityTestInfo.AvailabilityResult.Id = activitySpanId;

                if (regRaram.Type.Equals(typeof(AvailabilityTestInfo)))
                {
                    AvailabilityTestInfo actParam = (AvailabilityTestInfo) executingContext.Arguments[regRaram.Name];
                    actParam.AvailabilityResult.Id = activitySpanId;
                    actParam.SetStartTime(startTime);
                }
                else if (regRaram.Type.Equals(typeof(AvailabilityTelemetry)))
                {
                    AvailabilityTelemetry actParam = (AvailabilityTelemetry) executingContext.Arguments[regRaram.Name];
                    actParam.Id = activitySpanId;
                    actParam.Timestamp = startTime.ToUniversalTime();
                }
                else if (regRaram.Type.Equals(typeof(JObject)))
                {
                    JObject actParam = (JObject) executingContext.Arguments[regRaram.Name];
                    actParam["AvailabilityResult"]["Id"].Replace(JToken.FromObject(activitySpanId));
                    actParam["StartTime"].Replace(JToken.FromObject(startTime));
                    actParam["AvailabilityResult"]["Timestamp"].Replace(JToken.FromObject(startTime.ToUniversalTime()));
                }
                else
                {
                    throw new InvalidOperationException($"Unexpected managed parameter type: \"{regRaram.Type.FullName}\".");
                }
            }

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

            // Measure user time (plus the minimal runtime overhead within the bracket of this binding):
            DateTimeOffset endTime = DateTimeOffset.Now;

            // Stop activity:
            string activitySpadId = invocationState.ActivitySpan.SpanId.ToHexString();
            invocationState.ActivitySpan.Stop();

            // Get Function result (failed or not):
            bool errorOcurred = ! executedContext.FunctionResult.Succeeded;
            Exception error = errorOcurred 
                                    ? executedContext.FunctionResult.Exception
                                    : null;

            // Look at every paramater that was originally tagged with the attribute:
            foreach (FunctionInvocationState.Parameter registeredRaram in invocationState.Parameters.Values)
            {
                // Find the actual parameter value in the function arguments (named lookup):
                if (false == executedContext.Arguments.TryGetValue(registeredRaram.Name, out object functionOutputParam))
                {
                    throw new InvalidOperationException($"A parameter with name \"{Convert.NotNullOrWord(registeredRaram?.Name)}\" and"
                                                      + $" type \"{Convert.NotNullOrWord(registeredRaram?.Type)}\" was registered for"
                                                      + $" the function \"{Convert.NotNullOrWord(executedContext?.FunctionName)}\", but it was not found in the"
                                                      + $" actual argument list after the function invocation.");
                }

                if (functionOutputParam == null)
                {
                    throw new InvalidOperationException($"A parameter with name \"{Convert.NotNullOrWord(registeredRaram?.Name)}\" and"
                                                      + $" type \"{Convert.NotNullOrWord(registeredRaram?.Type)}\" was registered for"
                                                      + $" the function \"{Convert.NotNullOrWord(executedContext?.FunctionName)}\", and the corresponding value in the"
                                                      + $" actual argument list after the function invocation was null.");
                }

                // Based on parameter type, convert it to AvailabilityTestInfo and then process:

                bool functionOutputParamProcessed = false;

                {
                    // If this argument is a AvailabilityTestInfo:
                    var testInfoParameter = functionOutputParam as AvailabilityTestInfo;
                    if (testInfoParameter != null)
                    {
                        ProcessOutputParameter(endTime, errorOcurred, error, testInfoParameter, activitySpadId, cancelControl);
                        functionOutputParamProcessed = true;
                    }
                }

                {
                    // If this argument is a AvailabilityTelemetry:
                    var availabilityResultParameter = functionOutputParam as AvailabilityTelemetry;
                    if (availabilityResultParameter != null)
                    {
                        AvailabilityTestInfo testInfoParameter = Convert.AvailabilityTelemetryToAvailabilityTestInvocation(availabilityResultParameter);
                        ProcessOutputParameter(endTime, errorOcurred, error, testInfoParameter, activitySpadId, cancelControl);
                        functionOutputParamProcessed = true;
                    }
                }

                {
                    // If this argument is a JObject:
                    var jObjectParameter = functionOutputParam as JObject;
                    if (jObjectParameter != null)
                    {
                        // Can jObjectParameter be cnverted to a AvailabilityTestInfo (null if not):
                        AvailabilityTestInfo testInfoParameter = Convert.JObjectToAvailabilityTestInvocation(jObjectParameter);
                        if (testInfoParameter != null)
                        {
                            ProcessOutputParameter(endTime, errorOcurred, error, testInfoParameter, activitySpadId, cancelControl);
                            functionOutputParamProcessed = true;
                        }
                    }
                }

                if (false == functionOutputParamProcessed)
                {
                    throw new InvalidOperationException($"A parameter with name \"{Convert.NotNullOrWord(registeredRaram?.Name)}\" and"
                                                      + $" type \"{Convert.NotNullOrWord(registeredRaram?.Type)}\" was registered for"
                                                      + $" the function \"{Convert.NotNullOrWord(executedContext?.FunctionName)}\", and the corresponding value in the"
                                                      + $" actual argument list after the function invocation cannot be processed"
                                                      + $" ({Convert.NotNullOrWord(functionOutputParam?.GetType()?.Name)}).");
                }
            }

            // Make sure everyting we trracked is put on the wire, in case the Function runtime shuts down:
            _telemetryClient.Flush();

            return Task.CompletedTask;
        }

        private void ProcessOutputParameter(
                            DateTimeOffset endTime,
                            bool errorOcurred, 
                            Exception error, 
                            AvailabilityTestInfo functionOutputParam, 
                            string activitySpadId,
                            CancellationToken cancelControl)
        {
            if (errorOcurred)
            {
                // If the user code completed with an error or a timeout, then the Test resuls is always "fail":
                functionOutputParam.AvailabilityResult.Success = false;

                // Annotate exception and the availability result:
                OutputTelemetryFormat.AnnotateFunctionError(error, functionOutputParam);
                OutputTelemetryFormat.AnnotateAvailabilityResultWithErrorInfo(functionOutputParam, error);
            }

            // If user did not initialize Message, initialize it to default value according to the result:
            if (String.IsNullOrEmpty(functionOutputParam.AvailabilityResult.Message))
            {
                functionOutputParam.AvailabilityResult.Message = errorOcurred
                                                                ? IsUserCodeTimeout(error, cancelControl)
                                                                        ? OutputTelemetryFormat.DefaultResultMessage_Timeout
                                                                        : OutputTelemetryFormat.DefaultResultMessage_Error
                                                                : functionOutputParam.AvailabilityResult.Success
                                                                        ? OutputTelemetryFormat.DefaultResultMessage_Pass
                                                                        : OutputTelemetryFormat.DefaultResultMessage_Fail;
            }

            // If user did not initialize Duration, initialize it to default value according to the measurement:
            if (functionOutputParam.AvailabilityResult.Duration == TimeSpan.Zero)
            {
                functionOutputParam.AvailabilityResult.Duration = endTime - functionOutputParam.AvailabilityResult.Timestamp;
            }

            // Send the availability result to the backend:
            OutputTelemetryFormat.RemoveAvailabilityTestInfoIdentity(functionOutputParam.AvailabilityResult);
            functionOutputParam.AvailabilityResult.Id = activitySpadId;
            _telemetryClient.TrackAvailability(functionOutputParam.AvailabilityResult);
        }

        private void IdentifyManagedParameters(FunctionInvocationState invocationState, IReadOnlyDictionary<string, object> actualFunctionParameters)
        {
            int identifiedParameterCount = 0;
            // Look at each argument:
            if (actualFunctionParameters != null)
            {
                foreach (KeyValuePair<string, object> actualFunctionParameter in actualFunctionParameters)
                {
                    // Skip null value:
                    if (actualFunctionParameter.Value == null)
                    {
                        continue;
                    }

                    {
                        // If this argument is a AvailabilityTestInfo:
                        var testInfoParameter = actualFunctionParameter.Value as AvailabilityTestInfo;
                        if (testInfoParameter != null)
                        {
                            // Find registered parameter with the right ID, validate, and store its name:
                            Guid actualFunctionParameterId = testInfoParameter.Identity;
                            if (TryIdentifyAndValidateManagedParameter(invocationState, actualFunctionParameter.Key, actualFunctionParameter.Value, actualFunctionParameterId))
                            { 
                                identifiedParameterCount++;
                            }
                        }
                    }

                    {
                        // If this argument is a AvailabilityTelemetry:
                        var availabilityResultParameter = actualFunctionParameter.Value as AvailabilityTelemetry;
                        if (availabilityResultParameter != null)
                        {
                            // Find registered parameter with the right ID, validate, and store its name:
                            Guid actualFunctionParameterId = OutputTelemetryFormat.GetAvailabilityTestInfoIdentity(availabilityResultParameter);
                            if (TryIdentifyAndValidateManagedParameter(invocationState, actualFunctionParameter.Key, actualFunctionParameter.Value, actualFunctionParameterId))
                            {
                                identifiedParameterCount++;
                            }
                        }
                    }

                    {
                        // If this argument is a JObject:
                        var jObjectParameter = actualFunctionParameter.Value as JObject;
                        if (jObjectParameter != null)
                        {
                            // Can jObjectParameter be cnverted to a AvailabilityTestInfo (null if not):
                            AvailabilityTestInfo testInfoParameter = Convert.JObjectToAvailabilityTestInvocation(jObjectParameter);
                            if (testInfoParameter != null)
                            {
                                // Find registered parameter with the right ID, validate, and store its name:
                                Guid actualFunctionParameterId = testInfoParameter.Identity;
                                if (TryIdentifyAndValidateManagedParameter(invocationState, actualFunctionParameter.Key, actualFunctionParameter.Value, actualFunctionParameterId))
                                {
                                    identifiedParameterCount++;
                                }
                            }
                        }
                    }
                }
            }

            if (identifiedParameterCount != invocationState.Parameters.Count)
            {
                throw new InvalidOperationException($"{invocationState.Parameters.Count} parameters were marked with the {nameof(AvailabilityTestAttribute)},"
                                                  + $" but {identifiedParameterCount} parameters were identified during the actual function invocation.");
            }
        }

        private bool TryIdentifyAndValidateManagedParameter(FunctionInvocationState invocationState, string actualParamName, object actualParamValue, Guid actualParamId)
        {
            // Check if the actual param matches a registered managed param:
            if (false == invocationState.Parameters.TryGetValue(actualParamId, out FunctionInvocationState.Parameter registeredParam))
            {
                return false;
            }

            // Validate type match:
            if (false == actualParamValue.GetType().Equals(registeredParam.Type))
            {
                throw new InvalidProgramException($"The parameter with the identity \'{OutputTelemetryFormat.FormatGuid(actualParamId)}\'"
                                                + $" is expected to be of type {registeredParam.Type},"
                                                + $" but in reality it is of type {actualParamValue.GetType().Name}.");
            }

            // Validate ID uniqueness:
            if (registeredParam.Name != null)
            {
                throw new InvalidProgramException($"The parameter with the identity \'{OutputTelemetryFormat.FormatGuid(actualParamId)}\'"
                                                + $" has the name \'{actualParamName}\', but a parameter with that"
                                                + $" identify has already been encountered under the name \'{registeredParam.Name}\'.");
            }

            registeredParam.Name = actualParamName;
            return true;
        }


        private void ValidateactivitySpanId(string activitySpanId, AvailabilityTestInfo availabilityTestInfo)
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

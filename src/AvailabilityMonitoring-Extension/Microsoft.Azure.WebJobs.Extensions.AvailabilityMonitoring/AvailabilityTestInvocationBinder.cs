using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.Channel;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    internal class AvailabilityTestInvocationBinder : IValueBinder, IErrorAwareValueBinder
    {
        public static Type BoundValueType { get; } = typeof(AvailabilityTestInvocation);

        private static readonly Task CompletedTask = Task.FromResult(result: true);

        private const string DefaultResultMessage_Pass = "Passed: Coded Availability Test completed normally and reported Success.";
        private const string DefaultResultMessage_Fail = "Failed: Coded Availability Test completed normally and reported Failure.";
        private const string DefaultResultMessage_Error = "Error: An exception escaped from the Coded Availability Test.";
        private const string DefaultResultMessage_Timeout = "Error: The Coded Availability Test timed out.";

        private const string Moniker_AssociatedAvailabilityResultWithException = "AssociatedAvailabilityResult";
        private const string Moniker_AssociatedExceptionWithAvailabilityResult = "AssociatedException";

        private const string ErrorSetButNotSpecified = "Coded Availability Test completed abnormally, but no error information is available.";

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

        private static IDictionary<string, string> CreateExceptionCustomPropertiesForError(AvailabilityTelemetry availabilityResult)
        {
            var exceptionCustomProperties = new Dictionary<string, string>()
            {
                [$"{Moniker_AssociatedAvailabilityResultWithException}.Name"] = availabilityResult.Name,
                [$"{Moniker_AssociatedAvailabilityResultWithException}.RunLocation"] = availabilityResult.RunLocation,
                [$"{Moniker_AssociatedAvailabilityResultWithException}.Id"] = availabilityResult.Id,
            };

            return exceptionCustomProperties;
        }

        private static IDictionary<string, string> CreateAvailabilityResultCustomPropertiesForError(ExceptionTelemetry exception)
        {
            var availabilityResultCustomProperties = new Dictionary<string, string>()
            {
                [$"{Moniker_AssociatedExceptionWithAvailabilityResult}.ProblemId"] = Convert.NotNullOrWord(exception?.ProblemId),
            };

            return availabilityResultCustomProperties;
        }

        

        // @ToDo: Vaidate that an instance of an IValueBinder is created for each invocation of the function and instances are not reused.
        // If that ws nt the case, we could not keep state in here.

        private readonly AvailabilityTestAttribute _attribute;
        private readonly TelemetryClient _telemetryClient;

        private Activity _userActivity = null;
        private DateTimeOffset _startTime = default(DateTimeOffset);

        public AvailabilityTestInvocationBinder(AvailabilityTestAttribute attribute, TelemetryClient telemetryClient)
        {
            Validate.NotNull(attribute, nameof(attribute));

            _attribute = attribute;
            _telemetryClient = telemetryClient;
        }

        Type IValueProvider.Type
        {
            get
            {
                return BoundValueType;
            }
        }

        private AvailabilityTestInvocation CreateInvocationInfo()
        {
            var invocationInfo = new AvailabilityTestInvocation(
                                                _attribute.TestDisplayName,
                                                _attribute.TestArmResourceName,
                                                _attribute.LocationDisplayName,
                                                _attribute.LocationId,
                                                _startTime);
            return invocationInfo;
        }

        public Task<object> GetValueAsync()
        {
            _startTime = DateTimeOffset.Now;

            AvailabilityTestInvocation invocationInfo = CreateInvocationInfo();
            Task<object> wrappedInvocationInfo = Task.FromResult((object) invocationInfo);

            string activityName = FormatActivityName(invocationInfo.TestDisplayName, invocationInfo.LocationDisplayName);
            Activity userActivity = new Activity(activityName).Start();

            Activity prevActivity = Interlocked.CompareExchange(ref _userActivity, userActivity, null);
            if (prevActivity != null)
            {
                throw new InvalidOperationException($"Error initializing Coded Availability Test: {nameof(GetValueAsync)}(..) should"
                                                  + $" be called exactly once, but it was called at least twice. Activity span id"
                                                  + $" associated with the first invocation is: {FormatActivitySpanId(prevActivity)}.");
            }

            invocationInfo.AvailabilityResult.Id = FormatActivitySpanId(userActivity);

            return wrappedInvocationInfo;
        }

        public string ToInvokeString()
        {
            // @ToDo: What is the purpose of this method??
            throw new NotImplementedException();
        }

        public Task SetValueAsync(object valueToSet, CancellationToken cancelControl)
        {
            SetValueOrError(valueToSet, error: null, errorOcurred: false, cancelControl);

            // @ToDo Remove later:
            Console.WriteLine($"**** {nameof(AvailabilityTestInvocationBinder)}.{nameof(IValueBinder.SetValueAsync)}({valueToSet?.GetType()?.FullName ?? "null" })");
            Console.WriteLine($"**** valueToSet={(valueToSet == null ? "null" : JObject.FromObject(valueToSet).ToString())}");

            // This method completes synchronously:
            return CompletedTask;
        }

        public Task SetErrorAsync(object valueToSet, Exception error, CancellationToken cancelControl)
        {
            SetValueOrError(valueToSet, error, errorOcurred: true, cancelControl);

            // @ToDo Remove later:
            Console.WriteLine($"**** {nameof(AvailabilityTestInvocationBinder)}.{nameof(SetErrorAsync)}("
                            + $"{Convert.NotNullOrWord(valueToSet?.GetType()?.FullName)}, {Convert.NotNullOrWord(error?.GetType().Name)})");
            Console.WriteLine($"**** valueToSet={(valueToSet == null ? "null" : JObject.FromObject(valueToSet).ToString())}");
            Console.WriteLine($"**** error={Convert.NotNullOrWord(error?.ToString())}");

            // This method completes synchronously:
            return CompletedTask;
        }

        private void SetValueOrError(object valueToSet, Exception error, bool errorOcurred, CancellationToken cancelControl)
        {
            // Measure user time (plus the minimal runtime overhead within the bracket of this binding:
            DateTimeOffset endTime = DateTimeOffset.Now;

            // Fetch state:
            Activity userActivity = Interlocked.Exchange(ref _userActivity, null);
            string userActivitySpadId;

            // Stop activity & handle related errors (this also ensures that userActivity != null):
            try
            {
                userActivitySpadId = FormatActivitySpanId(userActivity);
                userActivity.Stop();
            } 
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error while stopping {nameof(_userActivity)}. Possible reasons include:"
                                                  + $" {nameof(GetValueAsync)}(..) was not called correctly,"
                                                  + $" one of {nameof(SetValueAsync)}(..)/{nameof(SetErrorAsync)}(..) was not called exactly once,"
                                                  + $" other reasons (see original exception).",
                                                    ex);
            }

            // Validate the specified valueToSet and convert it to AvailabilityTestInvocation:
            AvailabilityTestInvocation invocationInfo = ValidateValueToSet(valueToSet, errorOcurred, userActivitySpadId);

            if (errorOcurred)
            {
                // If the user code completed with an error or a timeout, then the Test resuls is always "fail":
                invocationInfo.AvailabilityResult.Success = false;

                // Track the exception:
                IDictionary<string, string> exProps = CreateExceptionCustomPropertiesForError(invocationInfo.AvailabilityResult);

                ITelemetry errorTelemetry = (error == null)
                                                        ? (ITelemetry) new TraceTelemetry(ErrorSetButNotSpecified, SeverityLevel.Error)
                                                        : (ITelemetry) new ExceptionTelemetry(error);
                foreach(KeyValuePair<string, string> prop in exProps)
                {
                    ((ISupportProperties) errorTelemetry).Properties[prop.Key] = prop.Value;
                }

                // @ToDo: How do we make sure that we do not double-track this exception?
                _telemetryClient.Track(errorTelemetry);

                // Add references about the exception we just tracked to the availability result:
                IDictionary<string, string> avResProps = CreateAvailabilityResultCustomPropertiesForError(errorTelemetry as ExceptionTelemetry);
                foreach (KeyValuePair<string, string> prop in avResProps)
                {
                    invocationInfo.AvailabilityResult.Properties[prop.Key] = prop.Value;
                }
            }

            // If user did not initialize Message, initialize it to default value according to the result:
            if (String.IsNullOrEmpty(invocationInfo.AvailabilityResult.Message))
            {
                invocationInfo.AvailabilityResult.Message = errorOcurred
                                                                ? IsUserCodeTimeout(error, cancelControl)
                                                                        ? DefaultResultMessage_Timeout
                                                                        : DefaultResultMessage_Error
                                                                : invocationInfo.AvailabilityResult.Success
                                                                        ? DefaultResultMessage_Pass
                                                                        : DefaultResultMessage_Fail;
            }

            // If user did not initialize Duration, initialize it to default value according to the measurement:
            if (invocationInfo.AvailabilityResult.Duration == TimeSpan.Zero)
            {
                invocationInfo.AvailabilityResult.Duration = endTime - invocationInfo.AvailabilityResult.Timestamp;
            }

            // Send the availability result to the backend:
            _telemetryClient.TrackAvailability(invocationInfo.AvailabilityResult);

            // Make sure everyting we trracked is put on the wire, if case the Function runtime shuts down:
            _telemetryClient.Flush();
        }

        private AvailabilityTestInvocation ValidateValueToSet(object valueToSet, bool errorOcurred, string userActivitySpadId)
        {
            try
            {
                // If (and only if) we are processing a user code error, we can handle an uninitialized value.
                // In that case, we re-create it based on the binding attribute:
                if (errorOcurred && valueToSet == null)
                {
                    AvailabilityTestInvocation recreatedValue = CreateInvocationInfo();
                    recreatedValue.AvailabilityResult.Id = userActivitySpadId;
                    valueToSet = recreatedValue;
                }

                // Now valueToSet must not be null:
                Validate.NotNull(valueToSet, nameof(valueToSet));

                // valueToSet must be of type AvailabilityTestInvocation:
                AvailabilityTestInvocation invocationInfo = valueToSet as AvailabilityTestInvocation;
                if (invocationInfo == null)
                {
                    throw new InvalidCastException($"The expected type of {nameof(valueToSet)} is \"{typeof(AvailabilityTestInvocation).FullName}\","
                                                 + $" but the actual type was \"{valueToSet.GetType().FullName}\".");
                }

                // valueToSet is a AvailabilityTestInvocation, so it contains an AvailabilityResult. Its id must match the userActivitySpadId:
                if (! userActivitySpadId.Equals(invocationInfo.AvailabilityResult.Id, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"The {nameof(invocationInfo.AvailabilityResult.Id)} of"
                                                + $" the {nameof(invocationInfo.AvailabilityResult)} specified"
                                                + $" in {nameof(valueToSet)} does not match the span Id of {nameof(_userActivity)}:"
                                                + $" {nameof(invocationInfo.AvailabilityResult)}.{nameof(invocationInfo.AvailabilityResult.Id)}=\"{invocationInfo.AvailabilityResult.Id}\";"
                                                + $" {nameof(userActivitySpadId)}=\"{invocationInfo.AvailabilityResult.Id}\".");
                }

                return invocationInfo;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"The {nameof(valueToSet)} passed to {(errorOcurred ? nameof(SetErrorAsync) : nameof(SetValueAsync))}(..) is invalid.", ex);
            }
        }

        private static bool IsUserCodeTimeout(Exception error, CancellationToken cancelControl)
        {
            bool IsUserCodeTimeout = (cancelControl == (error as TaskCanceledException)?.CancellationToken);
            return IsUserCodeTimeout;
        }
    }
}

using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    static internal class OutputTelemetryFormat
    {
        public const string DefaultResultMessage_Pass = "Passed: Coded Availability Test completed normally and reported Success.";
        public const string DefaultResultMessage_Fail = "Failed: Coded Availability Test completed normally and reported Failure.";
        public const string DefaultResultMessage_Error = "Error: An exception escaped from the Coded Availability Test.";
        public const string DefaultResultMessage_Timeout = "Error: The Coded Availability Test timed out.";

        public const string ErrorSetButNotSpecified = "Coded Availability Test completed abnormally, but no error information is available.";

        private const string Moniker_AssociatedAvailabilityResultWithException = "AssociatedAvailabilityResult";
        private const string Moniker_AssociatedExceptionWithAvailabilityResult = "AssociatedException";

        private const string Moniker_FunctionInstanceId = "_MS.FunctionInstanceId";

        public static IDictionary<string, string> CreateExceptionCustomPropertiesForError(AvailabilityTelemetry availabilityResult)
        {
            var exceptionCustomProperties = new Dictionary<string, string>()
            {
                [$"{Moniker_AssociatedAvailabilityResultWithException}.Name"] = availabilityResult.Name,
                [$"{Moniker_AssociatedAvailabilityResultWithException}.RunLocation"] = availabilityResult.RunLocation,
                [$"{Moniker_AssociatedAvailabilityResultWithException}.Id"] = availabilityResult.Id,
            };

            return exceptionCustomProperties;
        }

        public static IDictionary<string, string> CreateAvailabilityResultCustomPropertiesForError(ExceptionTelemetry exception)
        {
            var availabilityResultCustomProperties = new Dictionary<string, string>()
            {
                [$"{Moniker_AssociatedExceptionWithAvailabilityResult}.ProblemId"] = Convert.NotNullOrWord(exception?.ProblemId),
            };

            return availabilityResultCustomProperties;
        }

        public static void AddFunctionInstanceIdMarker(AvailabilityTelemetry availabilityResult, Guid functionInstanceId)
        {
            Validate.NotNull(availabilityResult, nameof(availabilityResult));

            availabilityResult.Properties[Moniker_FunctionInstanceId] = FormatFunctionInstanceId(functionInstanceId);
        }

        public static void RemoveFunctionInstanceIdMarker(AvailabilityTelemetry availabilityResult)
        {
            Validate.NotNull(availabilityResult, nameof(availabilityResult));

            availabilityResult.Properties.Remove(Moniker_FunctionInstanceId);
        }

        public static Guid GetFunctionInstanceId(AvailabilityTelemetry availabilityResult)
        {
            string functionInstanceIdStr = null;
            bool? hasId = availabilityResult?.Properties?.TryGetValue(Moniker_FunctionInstanceId, out functionInstanceIdStr);
            if (true == hasId)
            {
                if (functionInstanceIdStr != null && Guid.TryParse(functionInstanceIdStr, out Guid functionInstanceId))
                {
                    return functionInstanceId;
                }
            }

            return default(Guid);
        }

        public static string FormatFunctionInstanceId(Guid functionInstanceId)
        {
            return functionInstanceId.ToString("D").ToUpper();
        }
    }
}

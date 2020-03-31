using Microsoft.ApplicationInsights.DataContracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        private const string Moniker_AvailabilityTestInfoIdentity = "_MS.AvailabilityTestInfo.Identity";

        public static void AnnotateFunctionError(Exception error, AvailabilityTestInfo functionOutputParam)
        {
            if (error == null)
            {
                return;
            }

            error.Data[$"{Moniker_AssociatedAvailabilityResultWithException}.Name"] = functionOutputParam.AvailabilityResult.Name;
            error.Data[$"{Moniker_AssociatedAvailabilityResultWithException}.RunLocation"] = functionOutputParam.AvailabilityResult.RunLocation;
            error.Data[$"{Moniker_AssociatedAvailabilityResultWithException}.Id"] = functionOutputParam.AvailabilityResult.RunLocation;
        }


        public static void AnnotateAvailabilityResultWithErrorInfo(AvailabilityTestInfo functionOutputParam, Exception error)
        {
            string errorInfo = (error == null)
                                    ? ErrorSetButNotSpecified
                                    : $"{error.GetType().Name}: {error.Message}";

            functionOutputParam.AvailabilityResult.Properties[$"{Moniker_AssociatedExceptionWithAvailabilityResult}.Info"] = errorInfo;
        }

        public static void AddAvailabilityTestInfoIdentity(AvailabilityTelemetry availabilityResult, Guid functionInstanceId)
        {
            Validate.NotNull(availabilityResult, nameof(availabilityResult));

            availabilityResult.Properties[Moniker_AvailabilityTestInfoIdentity] = FormatGuid(functionInstanceId);
        }

        public static void RemoveAvailabilityTestInfoIdentity(AvailabilityTelemetry availabilityResult)
        {
            Validate.NotNull(availabilityResult, nameof(availabilityResult));

            availabilityResult.Properties.Remove(Moniker_AvailabilityTestInfoIdentity);
        }

        public static Guid GetAvailabilityTestInfoIdentity(AvailabilityTelemetry availabilityResult)
        {
            string functionInstanceIdStr = null;
            bool? hasId = availabilityResult?.Properties?.TryGetValue(Moniker_AvailabilityTestInfoIdentity, out functionInstanceIdStr);
            if (true == hasId)
            {
                if (functionInstanceIdStr != null && Guid.TryParse(functionInstanceIdStr, out Guid functionInstanceId))
                {
                    return functionInstanceId;
                }
            }

            return default(Guid);
        }

        public static string FormatGuid(Guid functionInstanceId)
        {
            return functionInstanceId.ToString("D").ToUpper();
        }

        public static string FormatActivityName(string testDisplayName, string locationDisplayName)
        {
            return String.Format("{0} / {1}", testDisplayName, locationDisplayName);
        }

        public static string FormatTimestamp(DateTimeOffset timestamp)
        {
            return JsonConvert.SerializeObject(timestamp);
        }
    }
}

using System;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.AvailabilityMonitoring;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    internal static class Convert
    {
        public static string AvailabilityTestInfoToString(AvailabilityTestInfo availabilityTestInfo)
        {
            Validate.NotNull(availabilityTestInfo, nameof(availabilityTestInfo));
            string str = JsonConvert.SerializeObject(availabilityTestInfo, Formatting.Indented);
            return str;
        }

        public static AvailabilityTelemetry AvailabilityTestInfoToAvailabilityTelemetry(AvailabilityTestInfo availabilityTestInfo)
        {
            Validate.NotNull(availabilityTestInfo, nameof(availabilityTestInfo));
            return availabilityTestInfo.DefaultAvailabilityResult;
        }

        public static AvailabilityTelemetry StringToAvailabilityTelemetry(string str)
        {
            if (str == null)
            {
                return null;
            }

            AvailabilityTelemetry availabilityResult = JsonConvert.DeserializeObject<AvailabilityTelemetry>(str);
            return availabilityResult;
        }
    }
}

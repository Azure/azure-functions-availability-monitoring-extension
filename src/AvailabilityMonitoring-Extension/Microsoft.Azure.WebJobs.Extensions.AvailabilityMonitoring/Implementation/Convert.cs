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

            try
            {
                AvailabilityTelemetry availabilityResult = JsonConvert.DeserializeObject<AvailabilityTelemetry>(str);
                return availabilityResult;
            }
            catch(Exception ex)
            {
                throw new FormatException($"Cannot parse the availability result."
                                        + $" The availability results must be a valid JSON representation of an {nameof(AvailabilityTelemetry)} object (partial objects are OK).",
                                          ex);
            }
        }
    }
}

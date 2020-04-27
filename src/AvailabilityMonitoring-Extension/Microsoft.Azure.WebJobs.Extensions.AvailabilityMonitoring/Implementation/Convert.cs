using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.ApplicationInsights.DataContracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    internal static class Convert
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetPropertyOrNullWord(AvailabilityTelemetry availabilityResult, string propertyName)
        {
            return Format.NotNullOrWord(GetPropertyOrNull(availabilityResult, propertyName));
        }

        public static string GetPropertyOrNull(AvailabilityTelemetry availabilityResult, string propertyName)
        {
            IDictionary<string, string> properties = availabilityResult?.Properties;
            if (properties == null || propertyName == null)
            {
                return null;
            }

            if (false == properties.TryGetValue(propertyName, out string propertyValue))
            {
                return null;
            }

            return propertyValue;
        }

        public static JObject AvailabilityTestInfoToJObject(AvailabilityTestInfo availabilityTestInfo)
        {
            Validate.NotNull(availabilityTestInfo, nameof(availabilityTestInfo));
            JObject jObject = JObject.FromObject(availabilityTestInfo);
            return jObject;
        }

        public static string AvailabilityTestInfoToString(AvailabilityTestInfo availabilityTestInfo)
        {
            Validate.NotNull(availabilityTestInfo, nameof(availabilityTestInfo));
            string str = JsonConvert.SerializeObject(availabilityTestInfo, Formatting.Indented);
            return str;
        }

        public static AvailabilityTestInfo JObjectToAvailabilityTestInfo(JObject availabilityTestInfo)
        {
            Validate.NotNull(availabilityTestInfo, nameof(availabilityTestInfo));

            try
            {
                AvailabilityTestInfo stronglyTypedTestInvocation = availabilityTestInfo.ToObject<AvailabilityTestInfo>();
                return stronglyTypedTestInvocation;
            }
            catch(Exception)
            {
                return null;
            }
        }

        public static AvailabilityTelemetry JObjectToAvailabilityTelemetry(JObject availabilityResult)
        {
            Validate.NotNull(availabilityResult, nameof(availabilityResult));

            try
            {
                AvailabilityTelemetry stronglyTypedAvailabilityTelemetry = availabilityResult.ToObject<AvailabilityTelemetry>();
                return stronglyTypedAvailabilityTelemetry;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static AvailabilityTelemetry AvailabilityTestInfoToAvailabilityTelemetry(AvailabilityTestInfo availabilityTestInfo)
        {
            Validate.NotNull(availabilityTestInfo, nameof(availabilityTestInfo));
            return availabilityTestInfo.AvailabilityResult;
        }

        public static AvailabilityTestInfo AvailabilityTelemetryToAvailabilityTestInfo(AvailabilityTelemetry availabilityResult)
        {
            Validate.NotNull(availabilityResult, nameof(availabilityResult));
            return new AvailabilityTestInfo(availabilityResult);
        }
    }
}

using System;
using Newtonsoft.Json;
using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.Azure.AvailabilityMonitoring
{
    public class AvailabilityTestInfo : IAvailabilityTestConfiguration
    {
        [JsonProperty]
        public string TestDisplayName { get; private set; }

        [JsonProperty]
        public DateTimeOffset StartTime { get; private set; }

        [JsonProperty]
        public AvailabilityTelemetry DefaultAvailabilityResult { get; private set; }

        public AvailabilityTestInfo()
        {
            this.TestDisplayName = null;
            this.StartTime = default;
            this.DefaultAvailabilityResult = null;
        }


        internal AvailabilityTestInfo(
                    string testDisplayName,
                    DateTimeOffset startTime,
                    AvailabilityTelemetry defaultAvailabilityResult)
        {
            Validate.NotNullOrWhitespace(testDisplayName, nameof(testDisplayName));
            Validate.NotNull(defaultAvailabilityResult, nameof(defaultAvailabilityResult));

            this.TestDisplayName = testDisplayName;
            this.StartTime = startTime;
            this.DefaultAvailabilityResult = defaultAvailabilityResult;
        }

        internal bool IsInitialized()
        {
            return (this.DefaultAvailabilityResult != null);
        }
        
        internal void CopyFrom(AvailabilityTestInfo availabilityTestInfo)
        {
            Validate.NotNull(availabilityTestInfo, nameof(availabilityTestInfo));

            Validate.NotNullOrWhitespace(availabilityTestInfo.TestDisplayName, "availabilityTestInfo.TestDisplayName");
            Validate.NotNull(availabilityTestInfo.DefaultAvailabilityResult, "availabilityTestInfo.DefaultAvailabilityResult");

            this.TestDisplayName = availabilityTestInfo.TestDisplayName;
            this.StartTime = availabilityTestInfo.StartTime;
            this.DefaultAvailabilityResult = availabilityTestInfo.DefaultAvailabilityResult;
        }
    }
}

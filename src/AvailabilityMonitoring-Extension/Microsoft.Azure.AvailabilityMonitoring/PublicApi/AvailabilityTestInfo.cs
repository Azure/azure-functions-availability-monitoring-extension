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
        public string LocationDisplayName { get; private set; }

        [JsonProperty]
        public string LocationId { get; private set; }

        [JsonProperty]
        public DateTimeOffset StartTime { get; private set; }

        [JsonProperty]
        public AvailabilityTelemetry DefaultAvailabilityResult { get; private set; }

        public AvailabilityTestInfo()
        {
            this.TestDisplayName = null;
            this.LocationDisplayName = null;
            this.LocationId = null;
            this.StartTime = default;
            this.DefaultAvailabilityResult = null;
        }


        internal AvailabilityTestInfo(
                    string testDisplayName,
                    string locationDisplayName, 
                    string locationId,
                    DateTimeOffset startTime,
                    AvailabilityTelemetry defaultAvailabilityResult)
        {
            Validate.NotNullOrWhitespace(testDisplayName, nameof(testDisplayName));
            Validate.NotNullOrWhitespace(locationDisplayName, nameof(locationDisplayName));
            Validate.NotNullOrWhitespace(locationId, nameof(locationId));
            Validate.NotNull(defaultAvailabilityResult, nameof(defaultAvailabilityResult));

            this.TestDisplayName = testDisplayName;
            this.LocationDisplayName = locationDisplayName;
            this.LocationId = locationId;
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
            Validate.NotNullOrWhitespace(availabilityTestInfo.LocationDisplayName, "availabilityTestInfo.LocationDisplayName");
            Validate.NotNullOrWhitespace(availabilityTestInfo.LocationId, "availabilityTestInfo.LocationId");
            Validate.NotNull(availabilityTestInfo.DefaultAvailabilityResult, "availabilityTestInfo.DefaultAvailabilityResult");

            this.TestDisplayName = availabilityTestInfo.TestDisplayName;
            this.LocationDisplayName = availabilityTestInfo.LocationDisplayName;
            this.LocationId = availabilityTestInfo.LocationId;
            this.StartTime = availabilityTestInfo.StartTime;
            this.DefaultAvailabilityResult = availabilityTestInfo.DefaultAvailabilityResult;
        }
    }
}

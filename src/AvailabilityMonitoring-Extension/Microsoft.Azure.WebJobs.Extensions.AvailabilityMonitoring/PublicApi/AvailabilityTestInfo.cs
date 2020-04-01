using Newtonsoft.Json;
using System;
using Microsoft.ApplicationInsights.DataContracts;


namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    public class AvailabilityTestInfo
    {
        [JsonProperty]
        public string TestDisplayName { get; }

        [JsonProperty]
        public string TestArmResourceName { get; }

        [JsonProperty]
        public string LocationDisplayName { get; }

        [JsonProperty]
        public string LocationId { get; }

        [JsonProperty]
        public DateTimeOffset StartTime { get; private set; }

        [JsonProperty]
        public AvailabilityTelemetry AvailabilityResult { get; }

        [JsonProperty]
        internal Guid Identity { get; }

        public AvailabilityTestInfo(
                    string testDisplayName,
                    string testArmResourceName, 
                    string locationDisplayName, 
                    string locationId)
        {
            Validate.NotNullOrWhitespace(testDisplayName, nameof(testDisplayName));
            Validate.NotNullOrWhitespace(testArmResourceName, nameof(testArmResourceName));
            Validate.NotNullOrWhitespace(locationDisplayName, nameof(locationDisplayName));
            Validate.NotNullOrWhitespace(locationId, nameof(locationId));

            this.TestDisplayName = testDisplayName;
            this.TestArmResourceName = testArmResourceName;
            this.LocationDisplayName = locationDisplayName;
            this.LocationId = locationId;
            this.StartTime = default(DateTimeOffset);

            this.AvailabilityResult = CreateNewAvailabilityResult();

            this.Identity = Guid.NewGuid();
        }

        public AvailabilityTestInfo(AvailabilityTelemetry availabilityResult)
            : this(Format.NotNullOrWord(availabilityResult?.Name),
                   Convert.GetPropertyOrNullWord(availabilityResult, "WebtestArmResourceName"),
                   Format.NotNullOrWord(availabilityResult?.RunLocation),
                   Convert.GetPropertyOrNullWord(availabilityResult, "WebtestLocationId"))
        {
            Validate.NotNull(availabilityResult, nameof(availabilityResult));

            this.StartTime = availabilityResult.Timestamp;
            this.AvailabilityResult = availabilityResult;
            this.Identity = Format.GetAvailabilityTestInfoIdentity(availabilityResult);
        }

        /// <summary>
        /// This is called by Newtonsoft.Json when converting from JObject.
        /// </summary>
        [JsonConstructor]
        private AvailabilityTestInfo(
                   string testDisplayName,
                   string testArmResourceName,
                   string locationDisplayName,
                   string locationId,
                   Guid identity,
                   DateTimeOffset startTime,
                   AvailabilityTelemetry availabilityResult)
        {
            Validate.NotNullOrWhitespace(testDisplayName, nameof(testDisplayName));
            Validate.NotNullOrWhitespace(testArmResourceName, nameof(testArmResourceName));
            Validate.NotNullOrWhitespace(locationDisplayName, nameof(locationDisplayName));
            Validate.NotNullOrWhitespace(locationId, nameof(locationId));
            Validate.NotNull(availabilityResult, nameof(availabilityResult));

            this.TestDisplayName = testDisplayName;
            this.TestArmResourceName = testArmResourceName;
            this.LocationDisplayName = locationDisplayName;
            this.LocationId = locationId;
            this.Identity = identity;
            this.StartTime = startTime;
            this.AvailabilityResult = availabilityResult;
        }

        internal void SetStartTime (DateTimeOffset startTime)
        {
            this.StartTime = startTime;
            this.AvailabilityResult.Timestamp = startTime.ToUniversalTime();
        }

        private AvailabilityTelemetry CreateNewAvailabilityResult()
        {
            const string mockApplicationInsightsAppId = "00000000-0000-0000-0000-000000000000";
            const string mockApplicationInsightsArmResourceName = "Application-Insights-Component";

            var availabilityResult = new AvailabilityTelemetry();

            availabilityResult.Timestamp = this.StartTime.ToUniversalTime();
            availabilityResult.Duration = TimeSpan.Zero;
            availabilityResult.Success = false;

            availabilityResult.Name = this.TestDisplayName;
            availabilityResult.RunLocation = this.LocationDisplayName;

            availabilityResult.Properties["SyntheticMonitorId"] = $"default_{this.TestArmResourceName}_{this.LocationId}";
            availabilityResult.Properties["WebtestArmResourceName"] = this.TestArmResourceName;
            availabilityResult.Properties["WebtestLocationId"] = this.LocationId;
            availabilityResult.Properties["SourceId"] = $"sid://{mockApplicationInsightsAppId}.visualstudio.com"
                                                                  + $"/applications/{mockApplicationInsightsArmResourceName}"
                                                                  + $"/features/{this.TestArmResourceName}"
                                                                  + $"/locations/{this.LocationId}";

            Format.AddAvailabilityTestInfoIdentity(availabilityResult, Identity);

            return availabilityResult;
        }
    }
}

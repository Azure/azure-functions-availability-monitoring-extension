using Newtonsoft.Json;
using System;
using Microsoft.ApplicationInsights.DataContracts;


namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    public class AvailabilityTestInvocation
    {
        public string TestDisplayName { get; }
        public string TestArmResourceName { get; }

        public string LocationDisplayName { get; }
        public string LocationId { get; }

        public DateTimeOffset StartTime { get; }

        public AvailabilityTelemetry AvailabilityResult { get; }

        public AvailabilityTestInvocation(
                    string testDisplayName,
                    string testArmResourceName, 
                    string locationDisplayName, 
                    string locationId, 
                    DateTimeOffset startTime)
        {
            Validate.NotNullOrWhitespace(testDisplayName, nameof(testDisplayName));
            Validate.NotNullOrWhitespace(testArmResourceName, nameof(testArmResourceName));
            Validate.NotNullOrWhitespace(locationDisplayName, nameof(locationDisplayName));
            Validate.NotNullOrWhitespace(locationId, nameof(locationId));

            this.TestDisplayName = testDisplayName;
            this.TestArmResourceName = testArmResourceName;
            this.LocationDisplayName = locationDisplayName;
            this.LocationId = locationId;
            this.StartTime = startTime;

            this.AvailabilityResult = CreateNewAvailabilityResult();
        }

        public AvailabilityTestInvocation(AvailabilityTelemetry availabilityResult)
            : this(Convert.NotNullOrWord(availabilityResult?.Name),
                   Convert.GetPropertyOrNullWord(availabilityResult, "WebtestArmResourceName"),
                   Convert.NotNullOrWord(availabilityResult?.RunLocation),
                   Convert.GetPropertyOrNullWord(availabilityResult, "WebtestLocationId"),
                   availabilityResult?.Timestamp ?? DateTimeOffset.Now)
        {
            Validate.NotNull(availabilityResult, nameof(availabilityResult));

            this.AvailabilityResult = availabilityResult;
        }

        /// <summary>
        /// This is called by Newtonsoft.Json when converting from JObject.
        /// </summary>
        [JsonConstructor]
        private AvailabilityTestInvocation(
                   string testDisplayName,
                   string testArmResourceName,
                   string locationDisplayName,
                   string locationId,
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
            this.StartTime = startTime;
            this.AvailabilityResult = availabilityResult;
        }


        //private void InitSampleValues()
        //{
        //    this.TestDisplayName = "User-specified Test name";
        //    this.TestArmResourceName = "user-specified-test-name-appinsights-component-name";

        //    this.LocationDisplayName = "Southeast Asia";
        //    this.LocationId = "apac-sg-sin-azr";

        //    this.StartTime = DateTimeOffset.Now;
        //}

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

            // availabilityResult.Properties["FullTestResultAvailable"] = "to what do we set this?";
            availabilityResult.Properties["SyntheticMonitorId"] = $"default_{this.TestArmResourceName}_{this.LocationId}";
            availabilityResult.Properties["WebtestArmResourceName"] = this.TestArmResourceName;
            availabilityResult.Properties["WebtestLocationId"] = this.LocationId;
            availabilityResult.Properties["SourceId"] = $"sid://{mockApplicationInsightsAppId}.visualstudio.com"
                                                                  + $"/applications/{mockApplicationInsightsArmResourceName}"
                                                                  + $"/features/{this.TestArmResourceName}"
                                                                  + $"/locations/{this.LocationId}";
            return availabilityResult;
        }
    }
}

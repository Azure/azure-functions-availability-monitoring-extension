using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Microsoft.Azure.AvailabilityMonitoring
{
    public static class HttpClientExtensions
    {
        // The names of the headers do not perfectly describe the intent, but we use them for compatibility reasons with existing headers used by GSM.
        // See here:
        // https://github.com/microsoft/ApplicationInsights-dotnet/blob/d1865fcba9ad9cbb27b623dd8a1bcdc112bf987e/WEB/Src/Web/Web/WebTestTelemetryInitializer.cs#L15
        
        private const string TestInvocationInstanceHeaderName = "SyntheticTest-RunId";
        private const string TestDescriptorHeaderName = "SyntheticTest-Location";

        public static void SetAvailabilityTestRequestHeaders(this HttpClient httpClient)
        {
            Validate.NotNull(httpClient, nameof(httpClient));

            Activity currentActivity = Activity.Current;
            if (currentActivity == null)
            {
                throw new InvalidOperationException($"Cannot set Availability Monitoring request headers for this {nameof(HttpClient)} because there is no"
                                                  + $" valid Activity.Current value representing an availability test span."
                                                  + $" Ensure that you are calling this API inside a valid and active {nameof(AvailabilityTestScope)},"
                                                  + $" or explicitly specify an {nameof(AvailabilityTestScope)} or an {nameof(Activity)}-span representing such a scope,"
                                                  + $" so that correct header values can be determined.");
            }

            httpClient.SetAvailabilityTestRequestHeaders(currentActivity);
        }

        public static void SetAvailabilityTestRequestHeaders(this HttpClient httpClient, AvailabilityTestScope availabilityTestScope)
        {
            Validate.NotNull(httpClient, nameof(httpClient));
            Validate.NotNull(availabilityTestScope, nameof(availabilityTestScope));

            string testInvocationInstance = availabilityTestScope.DistributedOperationId;
            string testDescriptor = availabilityTestScope.ActivitySpanOperationName;

            httpClient.SetAvailabilityTestRequestHeaders(testInvocationInstance, testDescriptor);
        }

        public static void SetAvailabilityTestRequestHeaders(this HttpClient httpClient, Activity activitySpan)
        {
            Validate.NotNull(httpClient, nameof(httpClient));
            Validate.NotNull(activitySpan, nameof(activitySpan));

            string testInvocationInstance = activitySpan.RootId;
            string testDescriptor = activitySpan.OperationName;

            if (! Format.AvailabilityTestSpanOperationNameObjectName.Equals(testDescriptor, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"The specified {nameof(activitySpan)} does not represent an activity span that was set up by an {nameof(AvailabilityTestScope)}.");
            }

            httpClient.SetAvailabilityTestRequestHeaders(testInvocationInstance, testDescriptor);
        }

        public static void SetAvailabilityTestRequestHeaders(this HttpClient httpClient, string availabilityTestInvocationInstance, string availabilityTestDescriptor)
        {
            Validate.NotNull(httpClient, nameof(httpClient));
            Validate.NotNullOrWhitespace(availabilityTestInvocationInstance, nameof(availabilityTestInvocationInstance));
            Validate.NotNullOrWhitespace(availabilityTestDescriptor, nameof(availabilityTestDescriptor));

            HttpRequestHeaders headers = httpClient.DefaultRequestHeaders;
            headers.Add(TestInvocationInstanceHeaderName, Format.SpellIfNull(availabilityTestInvocationInstance));
            headers.Add(TestDescriptorHeaderName, Format.SpellIfNull(availabilityTestDescriptor));
        }
    }
}

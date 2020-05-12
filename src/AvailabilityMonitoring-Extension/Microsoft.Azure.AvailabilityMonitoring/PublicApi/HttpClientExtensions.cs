using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Microsoft.Azure.AvailabilityMonitoring
{
    public static class HttpClientExtensions
    {
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

            string testInfoDescriptor = availabilityTestScope.ActivitySpanOperationName;
            string testInvocationInstanceDescriptor = availabilityTestScope.DistributedOperationId;

            httpClient.SetAvailabilityTestRequestHeaders(testInfoDescriptor, testInvocationInstanceDescriptor);
        }

        public static void SetAvailabilityTestRequestHeaders(this HttpClient httpClient, Activity activitySpan)
        {
            Validate.NotNull(httpClient, nameof(httpClient));
            Validate.NotNull(activitySpan, nameof(activitySpan));

            if (! activitySpan.IsAvailabilityTestSpan(out string testInfoDescriptor, out string testInvocationInstanceDescriptor))
            {
                throw new ArgumentException($"The specified {nameof(activitySpan)} does not represent an activity span that was set up by an {nameof(AvailabilityTestScope)}"
                                         + $" ({nameof(activitySpan.OperationName)}={Format.QuoteOrSpellNull(testInfoDescriptor)}).");
            }

            httpClient.SetAvailabilityTestRequestHeaders(testInfoDescriptor, testInvocationInstanceDescriptor);
        }

        public static void SetAvailabilityTestRequestHeaders(this HttpClient httpClient, string testInfoDescriptor, string testInvocationInstanceDescriptor)
        {
            Validate.NotNull(httpClient, nameof(httpClient));
            Validate.NotNullOrWhitespace(testInfoDescriptor, nameof(testInfoDescriptor));
            Validate.NotNullOrWhitespace(testInvocationInstanceDescriptor, nameof(testInvocationInstanceDescriptor));
            
            HttpRequestHeaders headers = httpClient.DefaultRequestHeaders;
            headers.Add(Format.AvailabilityTest.HttpHeaderNames.TestInfoDescriptor, Format.SpellIfNull(testInfoDescriptor));
            headers.Add(Format.AvailabilityTest.HttpHeaderNames.TestInvocationInstanceDescriptor, Format.SpellIfNull(testInvocationInstanceDescriptor));
        }
    }
}

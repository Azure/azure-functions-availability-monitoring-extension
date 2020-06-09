using System;
using System.ComponentModel;
using System.Net.Http;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.AvailabilityMonitoring
{
    public static class AvailabilityTest
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public class Logging
        {
            internal static readonly Logging SingeltonInstance = new Logging();

            private Logging() { }

            public bool UseConsoleIfNoLoggerAvailable { get; set; }

            internal ILogger CreateFallbackLogIfRequired(ILogger log)
            {
                if (log == null && this.UseConsoleIfNoLoggerAvailable)
                {
                    return new MinimalConsoleLogger();
                }

                return log;
            }
        }


        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Logging Log { get { return AvailabilityTest.Logging.SingeltonInstance; } }

        internal static AvailabilityTestScope StartNew(IAvailabilityTestInternalConfiguration testConfig, 
                                                       TelemetryConfiguration telemetryConfig, 
                                                       bool flushOnDispose, 
                                                       ILogger log, 
                                                       object logScope)
        {
            Validate.NotNull(testConfig, nameof(testConfig));
            return StartNew(testConfig.TestDisplayName, testConfig.LocationDisplayName, telemetryConfig, flushOnDispose, log, logScope);
        }

        public static AvailabilityTestScope StartNew(string testDisplayName,
                                                     string locationDisplayName,
                                                     TelemetryConfiguration telemetryConfig, 
                                                     bool flushOnDispose, 
                                                     ILogger log, 
                                                     object logScope)

        {
            log = AvailabilityTest.Log.CreateFallbackLogIfRequired(log);

            var testScope = new AvailabilityTestScope(testDisplayName, locationDisplayName, telemetryConfig, flushOnDispose, log, logScope);
            testScope.Start();

            return testScope;
        }

        public static HttpClient NewHttpClient(AvailabilityTestScope availabilityTestScope)
        {
            Validate.NotNull(availabilityTestScope, nameof(availabilityTestScope));

            var httpClient = new HttpClient();
            httpClient.SetAvailabilityTestRequestHeaders(availabilityTestScope);
            return httpClient;
        }

        public static HttpClient NewHttpClient()
        {
            var httpClient = new HttpClient();
            httpClient.SetAvailabilityTestRequestHeaders();
            return httpClient;
        }
    }
}

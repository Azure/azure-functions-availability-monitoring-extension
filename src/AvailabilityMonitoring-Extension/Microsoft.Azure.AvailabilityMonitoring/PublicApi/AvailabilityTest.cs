using System;
using System.ComponentModel;
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


        internal static AvailabilityTestScope StartNew(IAvailabilityTestConfiguration testConfig,
                                                       TelemetryConfiguration telemetryConfig,
                                                       bool flushOnDispose,
                                                       ILogger log)
        {
            return StartNew(testConfig, telemetryConfig, flushOnDispose, log, logScope: null);
        }

        internal static AvailabilityTestScope StartNew(IAvailabilityTestConfiguration testConfig,
                                                       TelemetryClient telemetryClient,
                                                       bool flushOnDispose,
                                                       ILogger log)
        {
            return StartNew(testConfig, telemetryClient, flushOnDispose, log, logScope: null);
        }

        internal static AvailabilityTestScope StartNew(IAvailabilityTestConfiguration testConfig, 
                                                       TelemetryConfiguration telemetryConfig, 
                                                       bool flushOnDispose, 
                                                       ILogger log, 
                                                       object logScope)
        {
            Validate.NotNull(testConfig, nameof(testConfig));
            return StartNew(testConfig.TestDisplayName, testConfig.LocationDisplayName, testConfig.LocationId, telemetryConfig, flushOnDispose, log, logScope);
        }

        internal static AvailabilityTestScope StartNew(IAvailabilityTestConfiguration testConfig, 
                                                       TelemetryClient telemetryClient, 
                                                       bool flushOnDispose, 
                                                       ILogger log,
                                                       object logScope)
        {
            Validate.NotNull(testConfig, nameof(testConfig));
            return StartNew(testConfig.TestDisplayName, testConfig.LocationDisplayName, testConfig.LocationId, telemetryClient, flushOnDispose, log, logScope);
        }

        public static AvailabilityTestScope StartNew(string testDisplayName,
                                                     string locationDisplayName,
                                                     string loctionId,
                                                     TelemetryConfiguration telemetryConfig,
                                                     bool flushOnDispose,
                                                     ILogger log)

        {
            return StartNew(testDisplayName, locationDisplayName, loctionId, telemetryConfig, flushOnDispose, log, logScope: null);
        }

        public static AvailabilityTestScope StartNew(string testDisplayName,
                                                     string locationDisplayName,
                                                     string loctionId,
                                                     TelemetryClient telemetryClient,
                                                     bool flushOnDispose,
                                                     ILogger log)
        {
            log = AvailabilityTest.Log.CreateFallbackLogIfRequired(log);

            var testScope = new AvailabilityTestScope(testDisplayName, locationDisplayName, loctionId, telemetryClient, flushOnDispose, log, logScope: null);
            testScope.Start();

            return testScope;
        }

        public static AvailabilityTestScope StartNew(string testDisplayName, 
                                                     string locationDisplayName, 
                                                     string loctionId, 
                                                     TelemetryConfiguration telemetryConfig, 
                                                     bool flushOnDispose, 
                                                     ILogger log, 
                                                     object logScope)

        {
            Validate.NotNull(telemetryConfig, nameof(telemetryConfig));

            var telemetryClient = new TelemetryClient(telemetryConfig);
            return StartNew(testDisplayName, locationDisplayName, loctionId, telemetryClient, flushOnDispose,log, logScope);
        }

        public static AvailabilityTestScope StartNew(string testDisplayName,
                                                     string locationDisplayName, 
                                                     string loctionId, 
                                                     TelemetryClient telemetryClient, 
                                                     bool flushOnDispose, 
                                                     ILogger log, 
                                                     object logScope)
        {
            log = AvailabilityTest.Log.CreateFallbackLogIfRequired(log);
           
            var testScope = new AvailabilityTestScope(testDisplayName, locationDisplayName, loctionId, telemetryClient, flushOnDispose, log, logScope);
            testScope.Start();

            return testScope;
        }
    }
}

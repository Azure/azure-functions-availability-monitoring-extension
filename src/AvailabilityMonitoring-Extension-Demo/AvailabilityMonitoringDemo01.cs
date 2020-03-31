using System;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace AvailabilityMonitoringExtensionDemo
{
    public class AvailabilityMonitoringDemo01
    {
        public const string Version = "1";
        public static readonly string FunctionName = typeof(AvailabilityMonitoringDemo01).FullName + $".Version.{AvailabilityMonitoringDemo01.Version}";

        private readonly TelemetryClient _telemetryClient;

        public AvailabilityMonitoringDemo01(TelemetryConfiguration telemetryConfig)
        {
            _telemetryClient = new TelemetryClient(telemetryConfig);
        }

        //[FunctionName("Availability-Monitoring-Demo-01-A-SimpleIoBinding")]
        //public async Task SimpleIoBinding(
        //                [TimerTrigger("5 */1 * * * *")] TimerInfo timerInfo,
        //                //[TimerTrigger("%FooBar%")] TimerInfo timerInfo,
        //                [AvailabilityTest(TestDisplayName = "Test Display Name",
        //                                  TestArmResourceName = "Test ARM Resource Name",
        //                                  LocationDisplayName = "Location Display Name",
        //                                  LocationId = "Location Id")] AvailabilityTestInfo testInvocation,
        //                ILogger log)
        //{
        //    log.LogInformation($"@@@@@@@@@@@@ \"{FunctionName}.{nameof(SimpleIoBinding)}\" started.");
        //    log.LogInformation($"@@@@@@@@@@@@ {ToString(timerInfo)}");

        //    testInvocation.AvailabilityResult.Name += " | Name was modified (A)";

        //    await Task.Delay(0);
        //}

        //        [FunctionName("Availability-Monitoring-Demo-01-B-IoBindingWithException")]
        //        public async Task IoBindingWithException(
        //                        [TimerTrigger("10 */1 * * * *")] TimerInfo timerInfo,
        //                        [AvailabilityTest(TestDisplayName = "Test Display Name",
        //                                          TestArmResourceName = "Test ARM Resource Name",
        //                                          LocationDisplayName = "Location Display Name",
        //                                          LocationId = "Location Id")] AvailabilityTestInfo testInvocation,
        //                        ILogger log)
        //        {
        //            const bool SimulateError = true;

        //            log.LogInformation($"############ \"{FunctionName}.{nameof(IoBindingWithException)}\" started.");
        //            log.LogInformation($"############ {ToString(timerInfo)}");

        //            testInvocation.AvailabilityResult.Name += " | Name modified before exception (B)";

        //            if (SimulateError)
        //            {
        //                throw new Exception("I AM A TEST EXCEPTION!! (B)");
        //            }

        //#pragma warning disable CS0162 // Unreachable code detected
        //            testInvocation.AvailabilityResult.Name += " | Name modified after exception site (B)";
        //#pragma warning restore CS0162 // Unreachable code detected

        //            await Task.Delay(0);
        //        }

        [FunctionName("Availability-Monitoring-Demo-01-C-BindingToJObject")]
        public async Task BindingToJObject(
                        //[TimerTrigger("15 */1 * * * *")] TimerInfo timerInfo,
                        [TimerTrigger("%AvailabilityTestInterval(1 minute)%")] TimerInfo timerInfo,
                        [AvailabilityTest(TestDisplayName = "Test Display Name",
                                          TestArmResourceName = "Test ARM Resource Name",
                                          LocationDisplayName = "Location Display Name",
                                          LocationId = "Location Id")] JObject testInvocation,
                        ILogger log)
        {
            log.LogInformation($"$$$$$$$$$$$$ \"{FunctionName}.{nameof(BindingToJObject)}\" started.");
            log.LogInformation($"$$$$$$$$$$$$ {ToString(timerInfo)}");
            log.LogInformation($"$$$$$$$$$$$$ testInvocation.GetType()={testInvocation?.GetType()?.FullName ?? "null"}");

            dynamic testInvocationInfo = testInvocation;

            log.LogDebug($"$$$$$$$$$$$$ testInvocationInfo.GetType()={testInvocationInfo?.GetType()?.FullName ?? "null"}");
            log.LogDebug($"$$$$$$$$$$$$ testInvocationInfo.AvailabilityResult.GetType()={testInvocationInfo?.AvailabilityResult?.GetType()?.FullName ?? "null"}");
            log.LogDebug($"$$$$$$$$$$$$ testInvocationInfo.AvailabilityResult.Name.GetType()={testInvocationInfo?.AvailabilityResult?.Name?.GetType()?.FullName ?? "null"}");

            testInvocationInfo.TestDisplayName += " | TestDisplayName was modified (C)";
            testInvocationInfo.AvailabilityResult.Name += " | AvailabilityResult.Name was modified (C)";

            testInvocationInfo.AvailabilityResult.Message = "This is a test message (C)";
            testInvocationInfo.AvailabilityResult.NonExistentMessage = "This is a test non-existent message (C)";
            testInvocationInfo.AvailabilityResult.Properties["Custom Dimension"] = "Custom Dimension Value (C)";

            await Task.Delay(0);
        }

        //[FunctionName("Availability-Monitoring-Demo-01-D-BindingToAvailabilityTelemetry")]
        //public async Task BindingToAvailabilityTelemetry(
        //                [TimerTrigger("20 */1 * * * *")] TimerInfo timerInfo,
        //                [AvailabilityTest(TestDisplayName = "Test Display Name",
        //                                  TestArmResourceName = "Test ARM Resource Name",
        //                                  LocationDisplayName = "Location Display Name",
        //                                  LocationId = "Location Id")] AvailabilityTelemetry availabilityResult,
        //                ILogger log)
        //{
        //    log.LogInformation($"%%%%%%%%%%%% \"{FunctionName}.{nameof(BindingToAvailabilityTelemetry)}\" started.");
        //    log.LogInformation($"%%%%%%%%%%%% {ToString(timerInfo)}");

        //    availabilityResult.Name += " | AvailabilityResult.Name was modified (D)";
        //    availabilityResult.Message = "This is a test message (D)";
        //    availabilityResult.Properties["Custom Dimension"] = "Custom Dimension Value (D)";

        //    await Task.Delay(0);
        //}



        private static string ToString(TimerInfo timerInfo)
        {
            string timerInfoString = String.Format("TimerInfo: Last = '{0}', Next = '{1}', LastUpdated = '{2}', IsPastDue = '{3}', NextOccurrences(5) = '{4}'.",
                                           timerInfo.ScheduleStatus.Last,
                                           timerInfo.ScheduleStatus.Next,
                                           timerInfo.ScheduleStatus.LastUpdated,
                                           timerInfo.IsPastDue,
                                           timerInfo.FormatNextOccurrences(5));
            return timerInfoString;
        }
    }
}

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.AvailabilityMonitoring;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring;
using Microsoft.Extensions.Logging;

namespace AvailabilityMonitoring_Extension_DemoFunction
{
    public static class CatDemoFunctions
    {
        [FunctionName("CatDemo-SimpleBinding")]
        [return: AvailabilityTestResult]
        public static async Task<bool> Run(
                            [TimerTrigger(AvailabilityTestInterval.Minute01)] TimerInfo notUsed,
                            ILogger log)
        {
            log.LogInformation($"[CatDemo-SimpleBinding] Run(..): C# Coded Availability Test Function executed at: {DateTime.Now}."
                             + $" ActivitySpanId = \"{Activity.Current.SpanId.ToHexString() ?? "null"}\".");

            string responseContent;
            using (HttpClient http = AvailabilityTest.NewHttpClient())
            {
                using (HttpResponseMessage response = await http.GetAsync("https://availabilitymonitoring-extension-monitoredappsample.azurewebsites.net/Home/MonitoredPage"))
                {
                    response.EnsureSuccessStatusCode();
                    responseContent = await response.Content.ReadAsStringAsync();
                }
            }

            bool hasExpectedContent = responseContent.Contains("<title>Monitored Page</title>", StringComparison.OrdinalIgnoreCase)
                                        && responseContent.Contains("(App Version Id: 2)", StringComparison.OrdinalIgnoreCase);

            return hasExpectedContent;
        }


        [FunctionName("CatDemo-ExplicitlySpecifyConfig")]
        [return: AvailabilityTestResult(TestDisplayName = "Validation Test 1", LocationDisplayName = "Validation Location 1", LocationId = "val-loc-1")]
        public static async Task<AvailabilityTelemetry> Run(
                            [TimerTrigger(AvailabilityTestInterval.Minute01)] TimerInfo notUsed,
                            //[TimerTrigger("*/5 * * * * *")] TimerInfo timerInfo,
                            [AvailabilityTestInfo] AvailabilityTestInfo testInfo,
                            ILogger log)
        {
            log.LogInformation($"[CatDemo-ExplicitlySpecifyConfig] Run(..): C#  Coded Availability Test Function executed at: {DateTime.Now}."
                             + $" ActivitySpanId = \"{Activity.Current.SpanId.ToHexString() ?? "null"}\";"
                             + $" TestDisplayName = \"{testInfo.TestDisplayName ?? "null"}\";"
                             + $" LocationDisplayName = \"{testInfo.LocationDisplayName ?? "null"}\";"
                             + $" LocationId = \"{testInfo.LocationId ?? "null"}\".");


            string responseContent;
            using (HttpClient http = AvailabilityTest.NewHttpClient())
            {
                using (HttpResponseMessage response = await http.GetAsync("https://availabilitymonitoring-extension-monitoredappsample.azurewebsites.net/Home/MonitoredPage"))
                {
                    response.EnsureSuccessStatusCode();
                    responseContent = await response.Content.ReadAsStringAsync();
                }
            }

            bool hasExpectedContent = responseContent.Contains("<title>Monitored Page</title>", StringComparison.OrdinalIgnoreCase)
                                        && responseContent.Contains("(App Version Id: 2)", StringComparison.OrdinalIgnoreCase);

            AvailabilityTelemetry result = testInfo.DefaultAvailabilityResult;

            result.Properties["UserProperty"] = "User Value";
            result.Success = hasExpectedContent;
            return result;
        }


        [FunctionName("CatDemo-ThrowsException")]
        [return: AvailabilityTestResult]
        public static bool RunWithException(
                            [TimerTrigger(AvailabilityTestInterval.Minute01)] TimerInfo notUsed,
                            //[TimerTrigger("*/5 * * * * *")] TimerInfo timerInfo,
                            ILogger log)
        {
            log.LogInformation($"[CatDemo-ThrowsException] RunWithException(..):  Coded Availability Test Function executed at: {DateTime.Now}."
                             + $" ActivitySpanId = \"{Activity.Current.SpanId.ToHexString() ?? "null"}\".");

            throw new HypotheticalTestException("This is a hypothetical test exception thrown by the user.");
        }


        [FunctionName("CatDemo-TimeoutError")]
        [return: AvailabilityTestResult]
        public static async Task<bool> RunWithTimeout(
                           [TimerTrigger(AvailabilityTestInterval.Minutes05)] TimerInfo notUsed,
                           ILogger log)
        {
            log.LogInformation($"[CatDemo-PlainSimplePrototype-ShouldTimeout] RunWithTimeout(..): C#  Coded Availability Test Function executed at: {DateTime.Now}."
                             + $" ActivitySpanId = \"{Activity.Current.SpanId.ToHexString() ?? "null"}\".");

            Console.WriteLine();
            Console.Write("Waiting to time out");

            DateTimeOffset startTime = DateTimeOffset.Now;

            TimeSpan passed = DateTimeOffset.Now - startTime;
            while (passed < TimeSpan.FromSeconds(120))
            {
                Console.Write($"...{(int) passed.TotalSeconds}");
                await Task.Delay(TimeSpan.FromSeconds(5));
                passed = DateTimeOffset.Now - startTime;
            }

            Console.WriteLine();
            return true;
        }

        public class HypotheticalTestException : Exception
        {
            public HypotheticalTestException() : base() {}
            public HypotheticalTestException(string msg) : base(msg) { }
        }
    }
}

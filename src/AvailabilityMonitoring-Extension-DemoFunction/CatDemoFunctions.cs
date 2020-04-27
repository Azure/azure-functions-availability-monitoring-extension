using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AvailabilityMonitoringExtension.PlainSimplePrototype;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring;
using Microsoft.Extensions.Logging;

namespace AvailabilityMonitoring_Extension_DemoFunction
{
    public static class CatDemoFunctions
    {
        //[FunctionName("CatDemo-SimpleBinding")]
        //public static async Task Run(
        //                    [TimerTrigger(AvailabilityTestInterval.Minute01)] TimerInfo timerInfo,
        //                    [AvailabilityTest] AvailabilityTestInfo testInfo,
        //                    ILogger log)
        //{
        //    log.LogInformation($"[CatDemo-SimpleBinding] Run(..): C# Timer trigger function executed at: {DateTime.Now}."
        //                     + $" ActivitySpanId = \"{Activity.Current.SpanId.ToHexString() ?? "null"}\";"
        //                     + $" TestDisplayName = \"{testInfo.TestDisplayName ?? "null"}\";"
        //                     + $" LocationDisplayName = \"{testInfo.LocationDisplayName ?? "null"}\";"
        //                     + $" LocationId = \"{testInfo.LocationId ?? "null"}\".");


        //    string responseContent;
        //    using (var http = new HttpClient())
        //    {
        //        using (HttpResponseMessage response = await http.GetAsync("https://availabilitymonitoring-extension-monitoredappsample.azurewebsites.net/Home/MonitoredPage"))
        //        {
        //            response.EnsureSuccessStatusCode();
        //            responseContent = await response.Content.ReadAsStringAsync();
        //        }
        //    }

        //    bool hasExpectedContent = responseContent.Contains("<title>Monitored Page</title>", StringComparison.OrdinalIgnoreCase)
        //                                && responseContent.Contains("(App Version Id: 2)", StringComparison.OrdinalIgnoreCase);

        //    testInfo.AvailabilityResult.Properties["UserProperty"] = "User Value";
        //    testInfo.AvailabilityResult.Success = hasExpectedContent;
        //}

        //[FunctionName("CatDemo-PlainSimplePrototype-ReturnValue")]
        //[return: AvailabilityTestResult(TestDisplayName = "An AvailabilityTestResult test!")]
        //public static async Task<AvailabilityTestResult> RunWithReturnValue(
        //                    [TimerTrigger("*/5 * * * * *")] TimerInfo timerInfo,
        //                    ILogger log)
        //{
        //    log.LogInformation($"[CatDemo-PlainSimplePrototype-ReturnValue] RunWithReturnValue(..): C# Timer trigger function executed at: {DateTime.Now}."
        //                     + $" ActivitySpanId = \"{Activity.Current.SpanId.ToHexString() ?? "null"}\".");


        //    return new AvailabilityTestResult
        //    {
        //        Success = true,
        //        Message = "This is a test message!"
        //    };
        //}

        [FunctionName("CatDemo-PlainSimplePrototype-ThrowsException")]
        [return: AvailabilityTestResult(TestDisplayName = "An AvailabilityTestResult test!")]
        public static async Task<AvailabilityTestResult> RunWithException(
                            [TimerTrigger("*/5 * * * * *")] TimerInfo timerInfo,
                            ILogger log)
        {
            log.LogInformation($"[CatDemo-PlainSimplePrototype-ThrowsException] RunWithException(..): C# Timer trigger function executed at: {DateTime.Now}."
                             + $" ActivitySpanId = \"{Activity.Current.SpanId.ToHexString() ?? "null"}\".");

            throw new HypotheticalTestException("This is hypothetical test exception.");
        }

        public class HypotheticalTestException : Exception
        {
            public HypotheticalTestException() : base() {}
            public HypotheticalTestException(string msg) : base(msg) { }
        }
    }
}

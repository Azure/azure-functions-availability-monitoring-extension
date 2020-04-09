using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring;
using Microsoft.Extensions.Logging;

namespace AvailabilityMonitoring_Extension_DemoFunction
{
    public static class CatDemoFunctions
    {
        [FunctionName("CatDemo-SimpleBinding")]
        public static async Task Run(
                            [TimerTrigger(AvailabilityTestInterval.Minute01)] TimerInfo timerInfo,
                            [AvailabilityTest] AvailabilityTestInfo testInfo,
                            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}."
                             + $" ActivitySpanId = \"{Activity.Current.SpanId.ToHexString() ?? "null"}\";"
                             + $" TestDisplayName = \"{testInfo.TestDisplayName ?? "null"}\";"
                             + $" LocationDisplayName = \"{testInfo.LocationDisplayName ?? "null"}\";"
                             + $" LocationId = \"{testInfo.LocationId ?? "null"}\".");


            string responseContent;
            using (var http = new HttpClient())
            {
                using (HttpResponseMessage response = await http.GetAsync("https://availabilitymonitoring-extension-monitoredappsample.azurewebsites.net/Home/MonitoredPage"))
                {
                    response.EnsureSuccessStatusCode();
                    responseContent = await response.Content.ReadAsStringAsync();
                }
            }

            bool hasExpectedContent = responseContent.Contains("<title>Monitored Page</title>", StringComparison.OrdinalIgnoreCase)
                                        && responseContent.Contains("(App Version Id: 2)", StringComparison.OrdinalIgnoreCase);
            
            testInfo.AvailabilityResult.Success = hasExpectedContent;
        }
    }
}

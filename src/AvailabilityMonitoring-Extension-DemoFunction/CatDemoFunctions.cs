using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring;
using Microsoft.Extensions.Logging;

namespace AvailabilityMonitoring_Extension_DemoFunction
{
    public static class CatDemoFunctions
    {
        [FunctionName("CatDemo-SimpleBinding")]
        public static void Run(
                            [TimerTrigger(AvailabilityTestInterval.Minute01)]TimerInfo timerInfo,
                            [AvailabilityTest] AvailabilityTestInfo testInfo,
                            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            testInfo.AvailabilityResult.Success = true;
        }
    }
}

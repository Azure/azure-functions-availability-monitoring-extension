using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;

namespace AvailabilityMonitoring_Extension_DemoFunction
{
    public static class TimeServer
    {
        public class TimeInfo
        {
            public DateTimeOffset UtcTime { get; set; }
            public DateTimeOffset LocalTime { get; set; }
            public string LocalTimeZone { get; set; }
            public string LocationInfo { get; set; }
        }

        [FunctionName("TimeServer")]
        public static IActionResult Run(
                            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
                            ILogger log)
        {
            Activity a = new Activity("My Activity");
            Activity b = a.Start();

            log.LogInformation("Function \"TimeServer\" was invoked.");
            TimeInfo timeInfo = GetLocalTimeInfo();
            return new OkObjectResult(timeInfo);
        }

        private static TimeInfo GetLocalTimeInfo()
        {
            DateTimeOffset now = DateTimeOffset.Now;
            TimeZoneInfo tz = TimeZoneInfo.Local;

            return new TimeInfo()
            {
                UtcTime = now.ToUniversalTime(),
                LocalTime = now,
                LocalTimeZone = tz.IsDaylightSavingTime(now) ? tz.DaylightName : tz.StandardName,
                LocationInfo = Environment.GetEnvironmentVariable("COMPUTERNAME") ?? Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") ?? "*"
            };
        }
    }
}

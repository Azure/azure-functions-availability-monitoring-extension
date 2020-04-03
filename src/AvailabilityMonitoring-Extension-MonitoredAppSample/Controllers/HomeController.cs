using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AvailabilityMonitoring_Extension_MonitoredAppSample.Models;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Collections;
using Newtonsoft.Json;
using System.Globalization;

namespace AvailabilityMonitoring_Extension_MonitoredAppSample.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> MonitoredPage()
        {
            var model = new MonitoredPageViewModel()
            {
                PublicTime = await GetRemoteTimeInfoAsync(),
                LocalTime = GetLocalTimeInfo(),
                FunctionTime = await GetFunctionTimeInfoAsync(),
                LocalEnvironment = GetEnvironment()
            };

            return View(model);
        }

        private async Task<MonitoredPageViewModel.TimeInfo> GetRemoteTimeInfoAsync()
        {
            string responseContent;

            using (var http = new HttpClient())
            {
                using (HttpResponseMessage response = await http.GetAsync("http://worldtimeapi.org/api/ip"))
                {
                    response.EnsureSuccessStatusCode();
                    responseContent = await response.Content.ReadAsStringAsync();
                }
            }

            dynamic timeInfo = JObject.Parse(responseContent);

            var timeInfoObj = new MonitoredPageViewModel.TimeInfo()
            {
                UtcTime = timeInfo.utc_datetime,
                LocalTime = timeInfo.datetime,
                LocalTimeZone = $"{timeInfo.timezone} ({timeInfo.abbreviation})",
                LocationInfo = "worldtimeapi.org"
            };

            // Is this really a bug in Newtonsoft that caused the time to be deserialized to local??
            timeInfoObj.UtcTime = timeInfoObj.UtcTime.ToUniversalTime();
            timeInfoObj.LocalTime = timeInfoObj.LocalTime.ToLocalTime();

            return timeInfoObj;
        }

        private async Task<MonitoredPageViewModel.TimeInfo> GetFunctionTimeInfoAsync()
        {
            string responseContent;

            using (var http = new HttpClient())
            {
                using (HttpResponseMessage response = await http.GetAsync("https://gregp-cat-test01.azurewebsites.net/api/TimeServer"))
                {
                    response.EnsureSuccessStatusCode();
                    responseContent = await response.Content.ReadAsStringAsync();
                }
            }

            MonitoredPageViewModel.TimeInfo timeInfo = JsonConvert.DeserializeObject<MonitoredPageViewModel.TimeInfo>(responseContent);
            return timeInfo;
        }

        private MonitoredPageViewModel.TimeInfo GetLocalTimeInfo()
        {
            DateTimeOffset now = DateTimeOffset.Now;
            TimeZoneInfo tz = TimeZoneInfo.Local;

            return new MonitoredPageViewModel.TimeInfo()
            {
                UtcTime = now.ToUniversalTime(),
                LocalTime = now,
                LocalTimeZone = tz.IsDaylightSavingTime(now) ? tz.DaylightName : tz.StandardName,
                LocationInfo = Environment.GetEnvironmentVariable("COMPUTERNAME") ?? Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") ?? "*"
            };
        }

        private static List<KeyValuePair<string, string>> GetEnvironment()
        {
            var environment = new Dictionary<object, object>();

            IDictionary environmentDict = Environment.GetEnvironmentVariables();
            foreach (DictionaryEntry entry in environmentDict)
            {
                environment.Add(entry.Key, entry.Value);
            }

            List<KeyValuePair<string, string>> orderedEnvironment = environment.Select(kvp => KeyValuePair.Create(kvp.Key.ToString(), kvp.Value.ToString()))
                                                                               .Select((kvp) => KeyValuePair.Create(kvp.Key,
                                                                                                                    kvp.Key.Contains("Key", StringComparison.OrdinalIgnoreCase)
                                                                                                                           || kvp.Key.Contains("Passw", StringComparison.OrdinalIgnoreCase)
                                                                                                                           || kvp.Key.Contains("Pass", StringComparison.OrdinalIgnoreCase)
                                                                                                                           || kvp.Key.Contains("Pwd", StringComparison.OrdinalIgnoreCase)
                                                                                                                           || kvp.Key.Contains("Sig", StringComparison.OrdinalIgnoreCase)
                                                                                                                           || kvp.Value.Contains("Key", StringComparison.OrdinalIgnoreCase)
                                                                                                                           || kvp.Value.Contains("Passw", StringComparison.OrdinalIgnoreCase)
                                                                                                                           || kvp.Value.Contains("Pass", StringComparison.OrdinalIgnoreCase)
                                                                                                                           || kvp.Value.Contains("Pwd", StringComparison.OrdinalIgnoreCase)
                                                                                                                           || kvp.Value.Contains("Sig", StringComparison.OrdinalIgnoreCase)
                                                                                                                       ? "..."
                                                                                                                       : kvp.Value))
                                                                               .OrderBy((kvp) => kvp.Key)
                                                                               .ToList();
            return orderedEnvironment;
        }
    }
}

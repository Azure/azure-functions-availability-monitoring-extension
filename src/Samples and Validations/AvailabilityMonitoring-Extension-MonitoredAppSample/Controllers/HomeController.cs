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
            MonitoredPageViewModel model = await (new TimeController()).GetAsync();
            return View(model);
        }

        
    }
}

using System;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring;
using Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring.Extensions;

[assembly: WebJobsStartup(typeof(AvailabilityMonitoringWebJobsStartup))]

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    internal class AvailabilityMonitoringWebJobsStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddAvailabilityMonitoring();
        }
    }
}

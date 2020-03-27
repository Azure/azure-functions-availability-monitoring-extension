using System;
using Microsoft.Azure.WebJobs;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    internal class AvailabilityTimerTriggerScheduleNameResolver : INameResolver
    {
        public string Resolve(string name)
        {
            return null;
        }
    }
}

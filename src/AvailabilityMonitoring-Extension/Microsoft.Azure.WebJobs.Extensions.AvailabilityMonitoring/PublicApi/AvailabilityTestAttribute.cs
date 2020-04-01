using System;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    [AttributeUsage(AttributeTargets.Parameter)]
    [Binding]
    public class AvailabilityTestAttribute : Attribute
    {
        public string TestDisplayName { get; set; }
        public string TestArmResourceName { get; set; }

        public string LocationDisplayName { get; set; }
        public string LocationId { get; set; }

    }
}
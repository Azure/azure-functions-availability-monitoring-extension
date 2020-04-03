using System;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class AvailabilityTestAttribute : Attribute
    {
        public static class DefaultConfigKeys
        {
            public const string TestDisplayName = "AvailabilityTest.TestDisplayName";
            public const string LocationDisplayName = "AvailabilityTest.LocationDisplayName";
            public const string LocationId = "AvailabilityTest.LocationId";
        }

        [AutoResolve]
        public string TestDisplayName { get; set; }

        //[AutoResolve]
        //[AppSetting(Default = "AvailabilityTestArmResourceName")]
        //public string TestArmResourceName { get; set; }

        [AutoResolve]
        public string LocationDisplayName { get; set; }

        [AutoResolve]
        public string LocationId { get; set; }
    }
}
using System;
using Microsoft.Azure.AvailabilityMonitoring;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    [Binding]
    [AttributeUsage(AttributeTargets.ReturnValue)]
    public class AvailabilityTestResultAttribute : Attribute, IAvailabilityTestConfiguration
    {
        internal const string BindingTypeName = "AvailabilityTestResult";

        [AutoResolve]
        public string TestDisplayName { get; set; }

        [AutoResolve]
        public string LocationDisplayName { get; set; }

        [AutoResolve]
        public string LocationId { get; set; }
    }
}
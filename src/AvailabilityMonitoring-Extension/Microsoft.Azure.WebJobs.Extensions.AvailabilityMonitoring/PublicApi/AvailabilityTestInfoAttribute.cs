using System;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class AvailabilityTestInfoAttribute : Attribute
    {
        internal const string BindingTypeName = "AvailabilityTestInfo";
    }
}
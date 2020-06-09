using Microsoft.Azure.AvailabilityMonitoring;
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    internal class FunctionMetadata
    {
        public IList<BindingMetadata> Bindings { get; set; }
    }

    internal class BindingMetadata : IAvailabilityTestConfiguration
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Direction { get; set; }
        public string TestDisplayName { get; set; }
    }
}

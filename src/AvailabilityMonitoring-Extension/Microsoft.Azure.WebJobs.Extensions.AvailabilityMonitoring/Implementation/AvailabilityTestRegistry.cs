using System;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    internal class AvailabilityTestRegistry
    {
        public AvailabilityTestFunctionRegistry Functions { get; }
        public AvailabilityTestInvocationRegistry Invocations { get; }

        public AvailabilityTestRegistry()
        {
            this.Functions = new AvailabilityTestFunctionRegistry();
            this.Invocations = new AvailabilityTestInvocationRegistry();
        }
    }
}

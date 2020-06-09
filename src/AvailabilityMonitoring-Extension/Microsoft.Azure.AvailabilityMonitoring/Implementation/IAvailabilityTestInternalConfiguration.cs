using Microsoft.Azure.AvailabilityMonitoring;
using System;

namespace Microsoft.Azure.AvailabilityMonitoring
{
    internal interface IAvailabilityTestInternalConfiguration
    {
        string TestDisplayName { get; }

        string LocationDisplayName { get; }
    }
}
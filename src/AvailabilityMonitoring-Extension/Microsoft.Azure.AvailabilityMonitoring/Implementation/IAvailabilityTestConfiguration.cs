using Microsoft.Azure.AvailabilityMonitoring;
using System;

namespace Microsoft.Azure.AvailabilityMonitoring
{
    internal interface IAvailabilityTestConfiguration 
    {
        string TestDisplayName { get; }
    }
}
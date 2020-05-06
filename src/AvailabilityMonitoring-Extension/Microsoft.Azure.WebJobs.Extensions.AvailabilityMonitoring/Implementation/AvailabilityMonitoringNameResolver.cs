using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    /// <summary>
    /// The format we are looking for:
    ///     AvailabilityTestInterval.Minute01
    ///     AvailabilityTestInterval.Minutes05
    ///     AvailabilityTestInterval.Minutes10
    ///     AvailabilityTestInterval.Minutes15
    /// We are NON-case-sensitive and ONLY the values 1, 5, 10 and 15 minutes are allowed.
    /// </summary>
    internal class AvailabilityMonitoringNameResolver : INameResolver
    {
        private readonly INameResolver _defaultNameResolver;

        public AvailabilityMonitoringNameResolver(IConfiguration config)
        {
            if (config != null)
            {
                _defaultNameResolver = new DefaultNameResolver(config);
            }
        }

        public string Resolve(string name)
        {
            // If this is a Availability Test Interval specification (has the right prefix), then resolve it:
            if (AvailabilityTestInterval.IsSpecification(name))
            {
                return ResolveAvailabilityTestInterval(name);
            }

            // If we have a default ame resolver, use it:
            if (_defaultNameResolver != null)
            {
                return _defaultNameResolver.Resolve(name);
            }

            // Do nothing:
            return name;
        }

        private string ResolveAvailabilityTestInterval(string testIntervalSpec)
        {
            int minuteInterval = AvailabilityTestInterval.Parse(testIntervalSpec);

            string cronSpec = AvailabilityTestInterval.CreateCronIntervalSpecWithRandomOffset(minuteInterval);
            return cronSpec;
        }
    }
}

using System;
using Microsoft.Azure.WebJobs;

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
    internal class AvailabilityTimerTriggerScheduleNameResolver : INameResolver
    {
        public string Resolve(string testIntervalSpec)
        {
            // Unless we have the right prefix, ignore the name - someone else will resolve it:
            if (false == AvailabilityTestInterval.IsSpecification(testIntervalSpec))
            {
                return testIntervalSpec;
            }

            int minuteInterval = AvailabilityTestInterval.Parse(testIntervalSpec);

            string cronSpec = AvailabilityTestInterval.CreateCronIntervalSpecWithRandomOffset(minuteInterval);
            return cronSpec;
        }
    }
}

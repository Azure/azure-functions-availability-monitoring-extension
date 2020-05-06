using System;
using System.Collections.Generic;

namespace AvailabilityMonitoring_Extension_MonitoredAppSample.Models
{
    public class MonitoredPageViewModel
    {
        public class TimeInfo
        {
            public DateTimeOffset UtcTime { get; set; }
            public DateTimeOffset LocalTime { get; set; }
            public string LocalTimeZone { get; set; }
            public string LocationInfo { get; set; }
        }

        public TimeInfo LocalTime { get; set; }

        public TimeInfo PublicTime { get; set; }

        public TimeInfo FunctionTime { get; set; }

        public IList<KeyValuePair<string, string>> LocalEnvironment { get; set; }


    }
}

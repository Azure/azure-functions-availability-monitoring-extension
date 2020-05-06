using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Azure.AvailabilityMonitoring;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    internal class AvailabilityTestInvocationState
    {
        private readonly Guid _functionInstanceId;
        private AvailabilityTestScope _availabilityTestScope = null;
        private AvailabilityResultAsyncCollector _resultCollector =  null;
        private IList<AvailabilityTestInfo> _availabilityTestInfos = null;

        public AvailabilityTestInvocationState(Guid functionInstanceId)
        {
            _functionInstanceId = functionInstanceId;
        }

        public void AttachTestScope(AvailabilityTestScope testScope)
        {
            Validate.NotNull(testScope, nameof(testScope));

            _availabilityTestScope = testScope;
        }

        public bool TryGetTestScope(out AvailabilityTestScope testScope)
        {
            testScope = _availabilityTestScope;
            return (testScope != null);
        }

        public void AttachResultCollector(AvailabilityResultAsyncCollector resultCollector)
        {
            Validate.NotNull(resultCollector, nameof(resultCollector));

            _resultCollector = resultCollector;
        }

        public bool TryGetResultCollector(out AvailabilityResultAsyncCollector resultCollector)
        {
            resultCollector = _resultCollector;
            return (resultCollector != null);
        }

        public void AttachTestInfo(AvailabilityTestInfo testInfo)
        {
            IList<AvailabilityTestInfo> testInfos = _availabilityTestInfos;
            if (testInfos == null)
            {
                var newTestInfos = new List<AvailabilityTestInfo>();
                IList<AvailabilityTestInfo> prevTestInfos = Interlocked.CompareExchange(ref _availabilityTestInfos, newTestInfos, null);
                testInfos = prevTestInfos ?? newTestInfos;
            }

            testInfos.Add(testInfo);
        }

        public bool TryGetTestInfos(out IEnumerable<AvailabilityTestInfo> testInfos)
        {
            testInfos = _availabilityTestInfos;
            return (testInfos != null);
        }
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.AvailabilityMonitoring;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    internal class AvailabilityResultAsyncCollector : IAsyncCollector<AvailabilityTelemetry>, IAsyncCollector<bool>
    {
        private AvailabilityTestScope _availabilityTestScope = null;
        
        public AvailabilityResultAsyncCollector()
        {
        }

        public void Initialize(AvailabilityTestScope availabilityTestScope)
        {
            Validate.NotNull(availabilityTestScope, nameof(availabilityTestScope));

            _availabilityTestScope = availabilityTestScope;
        }

        public Task AddAsync(bool availbilityResultSuccess, CancellationToken cancellationToken = default)
        {
            AvailabilityTestScope testScope = GetValidatedTestScope();

            testScope.Complete(availbilityResultSuccess);
            testScope.Dispose();

            return Task.CompletedTask;
        }

        public Task AddAsync(AvailabilityTelemetry availbilityResult, CancellationToken cancellationToken = default)
        {
            AvailabilityTestScope testScope = GetValidatedTestScope();

            testScope.Complete(availbilityResult);
            testScope.Dispose();

            return Task.CompletedTask;
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        private AvailabilityTestScope GetValidatedTestScope()
        {
            AvailabilityTestScope testScope = _availabilityTestScope;

            if (testScope == null)
            {
                throw new InvalidOperationException($"Cannot execute {nameof(AddAsync)}(..) on this instance of"
                                                  + $" {nameof(AvailabilityResultAsyncCollector)} becasue no"
                                                  + $" {nameof(AvailabilityTestScope)} was set by calling {nameof(Initialize)}(..).");
            }

            return testScope;
        }
    }
}
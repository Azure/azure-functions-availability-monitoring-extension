using System;
using System.Collections.Concurrent;
using Microsoft.Azure.AvailabilityMonitoring;
using Microsoft.Extensions.Logging;


namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    internal class AvailabilityTestInvocationRegistry
    {
        private readonly ConcurrentDictionary<Guid, AvailabilityTestInvocationState> _registeredInvocations;

        public AvailabilityTestInvocationRegistry()
        {
            _registeredInvocations = new ConcurrentDictionary<Guid, AvailabilityTestInvocationState>();
        }

        public AvailabilityTestInvocationState GetOrRegister(Guid functionInstanceId, ILogger log)
        {
            if (_registeredInvocations.TryGetValue(functionInstanceId, out AvailabilityTestInvocationState invocationState))
            {
                return invocationState;
            }

            return GetOrRegisterSlow(functionInstanceId, log);
        }

        public bool TryDeregister(Guid functionInstanceId, ILogger log, out AvailabilityTestInvocationState invocationState)
        {
            bool wasRegistered = _registeredInvocations.TryRemove(functionInstanceId, out invocationState);

            if (wasRegistered)
            {
                log = AvailabilityTest.Log.CreateFallbackLogIfRequired(log);

                log?.LogInformation($"A Coded Availability Test invocation instance was deregistered (completed):"
                                   + " {{ FunctionInstanceId=\"{FunctionInstanceId}\" }}",
                                    functionInstanceId);
            }

            return wasRegistered;
        }

        private AvailabilityTestInvocationState GetOrRegisterSlow(Guid functionInstanceId, ILogger log)
        {
            AvailabilityTestInvocationState newRegistration = null;
            AvailabilityTestInvocationState usedRegistration = _registeredInvocations.GetOrAdd(
                        functionInstanceId,
                        (id) =>
                        {
                            newRegistration = new AvailabilityTestInvocationState(id);
                            return newRegistration;
                        });

            if (usedRegistration == newRegistration)
            {
                log = AvailabilityTest.Log.CreateFallbackLogIfRequired(log);

                log?.LogInformation($"A new Coded Availability Test invocation instance was registered:"
                                   + " {{ FunctionInstanceId=\"{FunctionInstanceId}\" }}",
                                    functionInstanceId);
            }

            return usedRegistration;
        }
    }
}

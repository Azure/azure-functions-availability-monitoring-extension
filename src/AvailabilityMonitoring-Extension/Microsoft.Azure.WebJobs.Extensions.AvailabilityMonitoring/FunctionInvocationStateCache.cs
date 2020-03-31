using System;
using System.Collections.Concurrent;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    internal class FunctionInvocationStateCache
    {
        public static readonly FunctionInvocationStateCache SingeltonInstance = new FunctionInvocationStateCache();

        private readonly ConcurrentDictionary<Guid, FunctionInvocationState> _invocationStates = new ConcurrentDictionary<Guid, FunctionInvocationState>();


        public void RegisterFunctionInvocation(Guid functionInstanceId, AvailabilityTestInfo availabilityTestInfo, Type functionParameterType)
        {
            Validate.NotNull(availabilityTestInfo, nameof(availabilityTestInfo));
            Validate.NotNull(functionParameterType, nameof(functionParameterType));

            FunctionInvocationState invocationState = _invocationStates.GetOrAdd(functionInstanceId, (_) => new FunctionInvocationState(functionInstanceId));
            invocationState.AddManagedParameter(availabilityTestInfo, functionParameterType);
        }

        public bool TryStartFunctionInvocation(Guid functionInstanceId, out FunctionInvocationState invocationState)
        {
            bool isRegistered = _invocationStates.TryGetValue(functionInstanceId, out invocationState);
            if (! isRegistered)
            {
                return false;
            }

            try
            {
                invocationState.Transition(from: FunctionInvocationState.Stage.New, to: FunctionInvocationState.Stage.Started);
                return true;
            }
            catch (InvalidOperationException invOpEx)
            {
                throw new InvalidOperationException($"Cannot transition {nameof(FunctionInvocationState)}.{nameof(invocationState.CurrentStage)}"
                                                  + $" to \'{nameof(FunctionInvocationState.Stage.Started)}\'."
                                                  + $" This indicates that a {nameof(invocationState.FunctionInstanceId)} was not unique,"
                                                  + $" or that the assumption that the {nameof(FunctionInvocationManagementFilter)}"
                                                  + $" is invoked exactly once before and after the function invocation may be violated.",
                                                    invOpEx);
            }
        }

        public bool TryCompleteFunctionInvocation(Guid functionInstanceId, out FunctionInvocationState invocationState)
        {
            bool isRegistered = _invocationStates.TryGetValue(functionInstanceId, out invocationState);
            if (!isRegistered)
            {
                return false;
            }

            try
            {
                invocationState.Transition(from: FunctionInvocationState.Stage.Started, to: FunctionInvocationState.Stage.Completed);
                return true;
            }
            catch (InvalidOperationException invOpEx)
            {
                throw new InvalidOperationException($"Cannot transition {nameof(FunctionInvocationState)}.{nameof(invocationState.CurrentStage)}"
                                                  + $" to \'{nameof(FunctionInvocationState.Stage.Completed)}\'."
                                                  + $" This indicates that a {nameof(invocationState.FunctionInstanceId)} was not unique,"
                                                  + $" or that the assumption that the {nameof(FunctionInvocationManagementFilter)}"
                                                  + $" is invoked exactly once before and after the function invocation may be violated.",
                                                    invOpEx);
            }
        }

        public void RemoveFunctionInvocationRegistration(FunctionInvocationState invocationState)
        {
            Validate.NotNull(invocationState, nameof(invocationState));

            bool wasRegistered = _invocationStates.TryRemove(invocationState.FunctionInstanceId, out FunctionInvocationState _);
            if (! wasRegistered)
            {
                throw new InvalidOperationException($"Error removing the registration of a {nameof(FunctionInvocationState)}"
                                                 + $" for {nameof(invocationState.FunctionInstanceId)} = {invocationState.FormattedFunctionInstanceId}"
                                                 + $" from a {nameof(FunctionInvocationStateCache)}: A {nameof(FunctionInvocationState)} with"
                                                 + $" this {nameof(invocationState.FunctionInstanceId)} is not registered.");
            }

            try
            {
                invocationState.Transition(from: FunctionInvocationState.Stage.Completed, to: FunctionInvocationState.Stage.Removed);
            }
            catch (InvalidOperationException invOpEx)
            {
                throw new InvalidOperationException($"Cannot transition {nameof(FunctionInvocationState)}.{nameof(invocationState.CurrentStage)}"
                                                  + $" to \'{nameof(FunctionInvocationState.Stage.Removed)}\'.",
                                                    invOpEx);
            }
        }
    }
}
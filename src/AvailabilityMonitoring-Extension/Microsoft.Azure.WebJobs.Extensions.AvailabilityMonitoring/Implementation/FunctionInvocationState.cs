using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    internal class FunctionInvocationState
    {
        public enum Stage : Int32
        {
            New = 10,
            Started = 20,
            Completed = 30,
            Removed = 40
        }

        public class Parameter
        {
            public AvailabilityTestInfo AvailabilityTestInfo { get; }
            public Type Type { get; }

            public string Name { get; set; }

            public Parameter(AvailabilityTestInfo availabilityTestInfo, Type type)
            {
                AvailabilityTestInfo = availabilityTestInfo;
                Type = type;
                Name = null;
            }
        }

        private readonly ConcurrentDictionary<Guid, FunctionInvocationState.Parameter> _paremeters = new ConcurrentDictionary<Guid, FunctionInvocationState.Parameter>();
        private int _currentStage = (int)FunctionInvocationState.Stage.New;

        private Parameter _firstParameter = null;

        public Guid FunctionInstanceId { get; }
        public string FormattedFunctionInstanceId { get { return Format.Guid (FunctionInstanceId); } }

        public IReadOnlyDictionary<Guid, FunctionInvocationState.Parameter> Parameters { get { return _paremeters; } }

        public FunctionInvocationState.Stage CurrentStage { get { return (FunctionInvocationState.Stage)_currentStage; } }

        public DateTimeOffset StartTime { get; set; }

        public Activity ActivitySpan { get; set; }
        
        public IDisposable LoggerScope { get; set; }

        public FunctionInvocationState.Parameter FirstParameter { get { return _firstParameter; } }

        public string ActivitySpanName
        {
            get
            {
                Parameter firstParam = _firstParameter;
                if (firstParam == null)
                {
                    return null;
                }

                return Format.ActivitySpanName(firstParam.AvailabilityTestInfo.TestDisplayName, firstParam.AvailabilityTestInfo.LocationDisplayName);
            }
        }


        public FunctionInvocationState(Guid functionInstanceId)
        {
            this.FunctionInstanceId = functionInstanceId;
            this.ActivitySpan = null;
            this.LoggerScope = null;
        }

        public void AddManagedParameter(AvailabilityTestInfo availabilityTestInfo, Type functionParameterType)
        {
            Validate.NotNull(availabilityTestInfo, nameof(availabilityTestInfo));
            Validate.NotNull(functionParameterType, nameof(functionParameterType));

            if (CurrentStage != FunctionInvocationState.Stage.New)
            {
                throw new InvalidOperationException($"{nameof(AddManagedParameter)}(..) should only be called when {nameof(CurrentStage)} is"
                                                  + $" {Stage.New}; however, {nameof(CurrentStage)} is {CurrentStage}.");
            }

            var parameterInfo = new Parameter(availabilityTestInfo, functionParameterType);
            Interlocked.CompareExchange(ref _firstParameter, parameterInfo, null);
            _paremeters.TryAdd(availabilityTestInfo.Identity, parameterInfo);
        }

        public void Transition(FunctionInvocationState.Stage from, FunctionInvocationState.Stage to)
        {
            int fromStage = (int) from, toStage = (int) to;
            int prevStage = Interlocked.CompareExchange(ref _currentStage, toStage, fromStage);

            if (prevStage != fromStage)
            {
                throw new InvalidOperationException($"Error transitioning {nameof(CurrentStage)} of the {nameof(FunctionInvocationState)}"
                                                  + $" for {nameof(FunctionInstanceId)} = {FormattedFunctionInstanceId}"
                                                  + $" from \'{from}\' (={fromStage}) to \'{to}\' (={toStage}):"
                                                  + $" Original {nameof(CurrentStage)} value was {((Stage)prevStage)} (={prevStage}).");

                                                  
            }
        }
    }
}
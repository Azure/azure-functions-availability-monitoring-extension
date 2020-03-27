using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    internal class FunctionInvocationState
    {
        public enum Stage : Int32
        {
            New = 10,
            Registered = 20,
            Started = 30,
            Completed = 40,
            Removed = 50
        }

        private int _currentStage = (int) Stage.Registered;

        public Guid FunctionInstanceId { get; }
        public string FormattedFunctionInstanceId { get { return OutputTelemetryFormat.FormatFunctionInstanceId(FunctionInstanceId); } }

        public AvailabilityTestInvocation AvailabilityTestInfo { get; }

        public FunctionInvocationState.Stage CurrentStage { get { return (Stage)_currentStage; } }

        public DateTimeOffset StartTime { get; set; }

        public Activity ActivitySpan { get; set; }

        public FunctionInvocationState(Guid functionInstanceId, AvailabilityTestInvocation availabilityTestInfo)
        {
            _currentStage = (int) Stage.New;
            this.FunctionInstanceId = functionInstanceId;
            this.AvailabilityTestInfo = availabilityTestInfo;
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
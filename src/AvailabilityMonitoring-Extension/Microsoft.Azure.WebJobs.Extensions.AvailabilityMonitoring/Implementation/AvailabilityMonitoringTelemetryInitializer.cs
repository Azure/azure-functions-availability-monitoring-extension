using System;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    internal class AvailabilityMonitoringTelemetryInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetryItem)
        {
            Console.WriteLine();

            const string prefix = "*********** Availability Monitoring Telemetry Processor";
            Console.WriteLine($"{prefix}: Item type:                {Format.NotNullOrWord(telemetryItem?.GetType()?.Name)}");

            if (telemetryItem == null)
            {
                return;
            }

            Console.WriteLine($"{prefix}: Operation.Id:             {Format.NotNullOrWord(telemetryItem.Context.Operation.Id)}");
            Console.WriteLine($"{prefix}: Operation.Name:           {Format.NotNullOrWord(telemetryItem.Context.Operation.Name)}");
            Console.WriteLine($"{prefix}: Operation.ParentId:       {Format.NotNullOrWord(telemetryItem.Context.Operation.ParentId)}");
            Console.WriteLine($"{prefix}: Operation.SyntheticSource:{Format.NotNullOrWord(telemetryItem.Context.Operation.SyntheticSource)}");

            if (telemetryItem is TraceTelemetry traceTelemetry)
            {
                Console.WriteLine($"{prefix}: Message:                  {Format.NotNullOrWord(traceTelemetry.Message)}");
            }

            if (telemetryItem is RequestTelemetry requestTelemetry)
            {
                Console.WriteLine($"{prefix}: Name:                     {Format.NotNullOrWord(requestTelemetry.Name)}");
                Console.WriteLine($"{prefix}: Id:                       {Format.NotNullOrWord(requestTelemetry.Id)}");
            }

            if (telemetryItem is DependencyTelemetry dependencyTelemetry)
            {
                Console.WriteLine($"{prefix}: Name:                     {Format.NotNullOrWord(dependencyTelemetry.Name)}");
                Console.WriteLine($"{prefix}: Id:                       {Format.NotNullOrWord(dependencyTelemetry.Id)}");
            }

            if (telemetryItem is AvailabilityTelemetry availabilityTelemetry)
            {
                Console.WriteLine($"{prefix}: Name:                     {Format.NotNullOrWord(availabilityTelemetry.Name)}");
                Console.WriteLine($"{prefix}: Id:                       {Format.NotNullOrWord(availabilityTelemetry.Id)}");
            }

            Console.WriteLine();
            Console.WriteLine();
        }
    }
}

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
            //Console.WriteLine();

            //const string prefix = "*********** Availability Monitoring Telemetry Processor";
            //Console.WriteLine($"{prefix}: Item type:                {Format.SpellIfNull(telemetryItem?.GetType()?.Name)}");

            //if (telemetryItem == null)
            //{
            //    return;
            //}

            //Console.WriteLine($"{prefix}: Operation.Id:             {Format.SpellIfNull(telemetryItem.Context.Operation.Id)}");
            //Console.WriteLine($"{prefix}: Operation.Name:           {Format.SpellIfNull(telemetryItem.Context.Operation.Name)}");
            //Console.WriteLine($"{prefix}: Operation.ParentId:       {Format.SpellIfNull(telemetryItem.Context.Operation.ParentId)}");
            //Console.WriteLine($"{prefix}: Operation.SyntheticSource:{Format.SpellIfNull(telemetryItem.Context.Operation.SyntheticSource)}");

            //if (telemetryItem is TraceTelemetry traceTelemetry)
            //{
            //    Console.WriteLine($"{prefix}: Message:                  {Format.SpellIfNull(traceTelemetry.Message)}");
            //}

            //if (telemetryItem is RequestTelemetry requestTelemetry)
            //{
            //    Console.WriteLine($"{prefix}: Name:                     {Format.SpellIfNull(requestTelemetry.Name)}");
            //    Console.WriteLine($"{prefix}: Id:                       {Format.SpellIfNull(requestTelemetry.Id)}");
            //}

            //if (telemetryItem is DependencyTelemetry dependencyTelemetry)
            //{
            //    Console.WriteLine($"{prefix}: Name:                     {Format.SpellIfNull(dependencyTelemetry.Name)}");
            //    Console.WriteLine($"{prefix}: Id:                       {Format.SpellIfNull(dependencyTelemetry.Id)}");
            //}

            //if (telemetryItem is AvailabilityTelemetry availabilityTelemetry)
            //{
            //    Console.WriteLine($"{prefix}: Name:                     {Format.SpellIfNull(availabilityTelemetry.Name)}");
            //    Console.WriteLine($"{prefix}: Id:                       {Format.SpellIfNull(availabilityTelemetry.Id)}");
            //}

            //Console.WriteLine();
            //Console.WriteLine();
        }
    }
}

using System;
using System.Diagnostics;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.AvailabilityMonitoring;

namespace Microsoft.Azure.AvailabilityMonitoring.Extensions
{
    internal class AvailabilityMonitoringTelemetryInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetryItem)
        {
            if (telemetryItem == null)
            {
                return;
            }

            // We want to annotate telemetry that results from USER CODE of an Availability Test with SyntheticSource information.
            // This includes all telemetry types.
            // However, telemetry that results from the Functions Runtime, or from the Availability Monitoring Extension should not me annotated.
            // Since we use the AvailabilityTestScope's Activity for driving this logic, the above rule is not followed perfectly:
            // There are some telemetry items that are emitted within the scope of the availability test, but outside of user code.
            // We cannot avoid it, becasue we have co controll over execution order of the bindings.
            // Also, some trace telemetry items are affected by this.
            // Best-effort attmpt to excule such items:

            if (telemetryItem is TraceTelemetry traceTelemetry)
            {
                bool isUserTrace = false;
                if (traceTelemetry.Properties.TryGetValue("Category", out string categoryName))
                {
                    if (categoryName != null && categoryName.EndsWith(".User", StringComparison.OrdinalIgnoreCase))
                    {
                        isUserTrace = true;
                    }
                }

                if (! isUserTrace)
                {
                    return;
                }
            }

            // If we wanted to only include DependencyTelemetry, we would do it something like this:
            // if (false == (telemetryItem is DependencyTelemetry))
            //{
            //    return;
            //}

            Activity activity = Activity.Current;
            if (TryPopulateContextFromActivity(telemetryItem, activity))
            {
                return;
            }

            activity = activity?.Parent;
            TryPopulateContextFromActivity(telemetryItem, activity);
        }

        private bool TryPopulateContextFromActivity(ITelemetry telemetryItem, Activity activity)
        {
            if (activity != null && activity.IsAvailabilityTestSpan(out string testInfoDescriptor, out string testInvocationInstanceDescriptor))
            {
                // The Context fields below are chosen in-line with
                //   https://github.com/microsoft/ApplicationInsights-dotnet/blob/d1865fcba9ad9cbb27b623dd8a1bcdc112bf987e/WEB/Src/Web/Web/WebTestTelemetryInitializer.cs#L47
                // and the respective value format is adapted for the Coded Test scenario.

                telemetryItem.Context.Operation.SyntheticSource = Format.AvailabilityTest.TelemetryOperationSyntheticSourceMoniker;
                telemetryItem.Context.User.Id = $"{{{testInfoDescriptor}, OperationId=\"{testInvocationInstanceDescriptor}\"}}";
                telemetryItem.Context.Session.Id = testInvocationInstanceDescriptor;

                return true;
            }

            return false;
        }
    }
}

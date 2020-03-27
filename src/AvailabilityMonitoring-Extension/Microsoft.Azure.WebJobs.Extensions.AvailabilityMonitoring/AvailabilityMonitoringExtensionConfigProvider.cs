using System;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    [Extension("AvailabilityMonitoring")]
    internal class AvailabilityMonitoringExtensionConfigProvider : IExtensionConfigProvider
    {
        private readonly TelemetryClient _telemetryClient;

        public AvailabilityMonitoringExtensionConfigProvider(TelemetryConfiguration telemetryConfig)
        {
            _telemetryClient = new TelemetryClient(telemetryConfig);
        }

        public void Initialize(ExtensionConfigContext context)
        {
            Validate.NotNull(context, nameof(context));

            // FluentBindingRule<ApiAvailabilityTest> is marked as Obsolete, yet it is the type returned from AddBindingRule(..)
            // We could use "var", but one should NEVER use "var" except in Lync expressions
            // or when the type is clear from the *same* line to an unfamiliar reader.
            // Neither is the case, so we use the type explicitly and work around the obsolete-warning.
#pragma warning disable CS0618 
            FluentBindingRule<AvailabilityTestAttribute> rule = context.AddBindingRule<AvailabilityTestAttribute>();
#pragma warning restore CS0618 

            rule.BindToInput<AvailabilityTestInvocation>(CreateAvailabilityTestInvocation);
            rule.BindToInput<AvailabilityTelemetry>(CreateAvailabilityTelemetry);
            rule.BindToInput<JObject>(CreateJObject);
        }

        private static Task<AvailabilityTestInvocation> CreateAvailabilityTestInvocation(AvailabilityTestAttribute attribute, ValueBindingContext context)
        {
            Validate.NotNull(attribute, nameof(attribute));
            Validate.NotNull(context, nameof(context));

            Guid functionInstanceId = context.FunctionInstanceId;

            AvailabilityTestInvocation invocationInfo = new AvailabilityTestInvocation(
                                                                    attribute.TestDisplayName,
                                                                    attribute.TestArmResourceName,
                                                                    attribute.LocationDisplayName,
                                                                    attribute.LocationId,
                                                                    functionInstanceId);

            var functionInvocationState = new FunctionInvocationState(functionInstanceId, invocationInfo);
            FunctionInvocationStateCache.SingeltonInstance.RegisterFunctionInvocation(functionInvocationState);

            return Task.FromResult(invocationInfo);
        }

        private static Task<AvailabilityTelemetry> CreateAvailabilityTelemetry(AvailabilityTestAttribute attribute, ValueBindingContext context)
        {
            AvailabilityTestInvocation invocationInfo = CreateAvailabilityTestInvocation(attribute, context).Result;
            return Task.FromResult(Convert.AvailabilityTestInvocationToAvailabilityTelemetry(invocationInfo));
        }

        private static Task<JObject> CreateJObject(AvailabilityTestAttribute attribute, ValueBindingContext context)
        {
            AvailabilityTestInvocation invocationInfo = CreateAvailabilityTestInvocation(attribute, context).Result;
            return Task.FromResult(Convert.AvailabilityTestInvocationToJObject(invocationInfo));
        }
    }
}

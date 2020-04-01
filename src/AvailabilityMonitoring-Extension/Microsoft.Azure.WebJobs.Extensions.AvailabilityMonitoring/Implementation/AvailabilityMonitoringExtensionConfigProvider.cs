using System;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    [Extension("AvailabilityMonitoring")]
    internal class AvailabilityMonitoringExtensionConfigProvider : IExtensionConfigProvider
    {
        public AvailabilityMonitoringExtensionConfigProvider()
        {
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

            rule.BindToInput<AvailabilityTestInfo>(CreateAvailabilityTestInvocation);
            rule.BindToInput<AvailabilityTelemetry>(CreateAvailabilityTelemetry);
            rule.BindToInput<JObject>(CreateJObject);
        }

        private static Task<AvailabilityTestInfo> CreateAvailabilityTestInvocation(AvailabilityTestAttribute attribute, ValueBindingContext context)
        {
            Validate.NotNull(attribute, nameof(attribute));
            Validate.NotNull(context, nameof(context));

            AvailabilityTestInfo invocationInfo = CreateAndRegisterInvocation(attribute, context.FunctionInstanceId, typeof(AvailabilityTestInfo));
            return Task.FromResult(invocationInfo);
        }

        private static Task<AvailabilityTelemetry> CreateAvailabilityTelemetry(AvailabilityTestAttribute attribute, ValueBindingContext context)
        {
            Validate.NotNull(attribute, nameof(attribute));
            Validate.NotNull(context, nameof(context));

            AvailabilityTestInfo invocationInfo = CreateAndRegisterInvocation(attribute, context.FunctionInstanceId, typeof(AvailabilityTelemetry));
            return Task.FromResult(Convert.AvailabilityTestInvocationToAvailabilityTelemetry(invocationInfo));
        }

        private static Task<JObject> CreateJObject(AvailabilityTestAttribute attribute, ValueBindingContext context)
        {
            Validate.NotNull(attribute, nameof(attribute));
            Validate.NotNull(context, nameof(context));

            AvailabilityTestInfo invocationInfo = CreateAndRegisterInvocation(attribute, context.FunctionInstanceId, typeof(JObject));
            return Task.FromResult(Convert.AvailabilityTestInvocationToJObject(invocationInfo));
        }

        private static AvailabilityTestInfo CreateAndRegisterInvocation(AvailabilityTestAttribute attribute, Guid functionInstanceId, Type functionParameterType)
        {
            var availabilityTestInfo = new AvailabilityTestInfo(attribute.TestDisplayName,
                                                                attribute.TestArmResourceName,
                                                                attribute.LocationDisplayName,
                                                                attribute.LocationId);

            FunctionInvocationStateCache.SingeltonInstance.RegisterFunctionInvocation(functionInstanceId, availabilityTestInfo, functionParameterType);
            return availabilityTestInfo;
        }
    }
}

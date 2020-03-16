using System;
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

            rule.BindToValueProvider(CreateAvailabilityTestInvocationBinder);
        }

        private Task<IValueBinder> CreateAvailabilityTestInvocationBinder(AvailabilityTestAttribute attribute, Type type)
        {
            Validate.NotNull(attribute, nameof(attribute));
            Validate.NotNull(type, nameof(type));

            if (AvailabilityTestInvocationBinder.BoundValueType.IsAssignableFrom(type))
            {
                var binder = new AvailabilityTestInvocationBinder(attribute, _telemetryClient);
                return Task.FromResult((IValueBinder) binder);
            }
            else if (ConverterBinder<AvailabilityTelemetry, AvailabilityTestInvocation>.BoundValueType.IsAssignableFrom(type))
            {
                var binder = new ConverterBinder<AvailabilityTelemetry, AvailabilityTestInvocation>(
                                        new AvailabilityTestInvocationBinder(attribute, _telemetryClient),
                                        Convert.AvailabilityTestInvocationToAvailabilityTelemetry,
                                        Convert.AvailabilityTelemetryToAvailabilityTestInvocation);
                return Task.FromResult((IValueBinder) binder);
            }
            else if (ConverterBinder<JObject, AvailabilityTestInvocation>.BoundValueType.IsAssignableFrom(type))
            {
                var binder = new ConverterBinder<JObject, AvailabilityTestInvocation>(
                                        new AvailabilityTestInvocationBinder(attribute, _telemetryClient),
                                        Convert.AvailabilityTestInvocationToJObject,
                                        Convert.JObjectToAvailabilityTestInvocation);
                return Task.FromResult((IValueBinder) binder);
            }
            else
            {
                // @ToDo Test that IsAssignableFrom stuff!

                throw new InvalidOperationException($"Trying to use {nameof(AvailabilityTestAttribute)} to bind a value of type \"{type.FullName}\"."
                                                  + $" This attribute can only bind values of the following types:"
                                                  + $" \"{AvailabilityTestInvocationBinder.BoundValueType.FullName}\","
                                                  + $" \"{ConverterBinder<AvailabilityTelemetry, AvailabilityTestInvocation>.BoundValueType.FullName}\","
                                                  + $" \"{ConverterBinder<JObject, AvailabilityTestInvocation>.BoundValueType.FullName}\".");
            }
        }
    }
}

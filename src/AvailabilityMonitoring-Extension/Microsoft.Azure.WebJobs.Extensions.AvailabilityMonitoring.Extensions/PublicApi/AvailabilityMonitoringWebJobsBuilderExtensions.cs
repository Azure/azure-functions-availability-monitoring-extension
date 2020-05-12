using System;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.AvailabilityMonitoring;
using Microsoft.Azure.AvailabilityMonitoring.Extensions;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring.Extensions
{
    public static class AvailabilityMonitoringWebJobsBuilderExtensions
    {
        public static IWebJobsBuilder AddAvailabilityMonitoring(this IWebJobsBuilder builder)
        {
            Validate.NotNull(builder, nameof(builder));

            IServiceCollection serviceCollection = builder.Services;

            serviceCollection.AddSingleton<INameResolver, AvailabilityMonitoringNameResolver>();
            serviceCollection.AddSingleton<ITelemetryInitializer, AvailabilityMonitoringTelemetryInitializer>();

            serviceCollection.AddSingleton<AvailabilityTestRegistry>();

// Type 'IFunctionFilter' (and other Filter-related types) is marked as preview/obsolete,
// but the guidance from the Azure Functions team is to use it, so we disable the warning.
#pragma warning disable CS0618
            serviceCollection.AddSingleton<IFunctionFilter, FunctionInvocationManagementFilter>();
#pragma warning restore CS0618

            builder.AddExtension<AvailabilityMonitoringExtensionConfigProvider>();
            return builder;
        }
    }
}



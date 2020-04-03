using System;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring.Extensions
{
    public static class AvailabilityMonitoringWebJobsBuilderExtensions
    {
        public static IWebJobsBuilder AddAvailabilityMonitoring(this IWebJobsBuilder builder)
        {
            Validate.NotNull(builder, nameof(builder));

            builder.AddExtension<AvailabilityMonitoringExtensionConfigProvider>();
            IServiceCollection serviceCollection = builder.Services;

            serviceCollection.AddSingleton<INameResolver, AvailabilityMonitoringNameResolver>();

#pragma warning disable CS0618 // Type or member is obsolete (Filter-related types are obsolete, but we want to use them)
            serviceCollection.AddSingleton<IFunctionFilter, FunctionInvocationManagementFilter>();
#pragma warning restore CS0618 // Type or member is obsolete (Filter-related types are obsolete, but we want to use them)

            return builder;
        }
    }
}



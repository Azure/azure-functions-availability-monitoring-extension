using System;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    public static class AvailabilityMonitoringWebJobsBuilderExtensions
    {
        public static IWebJobsBuilder AddAvailabilityMonitoring(this IWebJobsBuilder builder)
        {
            Validate.NotNull(builder, nameof(builder));

            builder.AddExtension<AvailabilityMonitoringExtensionConfigProvider>();
            return builder;
        }
    }
}

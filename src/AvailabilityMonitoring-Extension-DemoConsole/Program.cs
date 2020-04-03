using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring.Extensions;

namespace AvailabilityMonitoringExtensionDemo
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine($"Starting {typeof(Program).FullName}");

            Console.WriteLine($"Initializing HostBuilder.");

            IHostBuilder builder = new HostBuilder()
                   .UseEnvironment("Development")
                   .ConfigureAppConfiguration((configBuilder) =>
                        {
                            configBuilder.AddJsonFile("local.settings.json");
                        })
                   .ConfigureWebJobs((webJobsBuilder) =>
                        {
                           webJobsBuilder
                               .AddAzureStorageCoreServices()
                               .AddExecutionContextBinding()
                               .AddTimers()
                               .AddAvailabilityMonitoring();
                        })
                    .ConfigureLogging((context, loggingBuilder) =>
                        {
                            loggingBuilder.SetMinimumLevel(LogLevel.Debug);
                            loggingBuilder.AddConsole();

                            string appInsightsInstrumentationKey = context.Configuration["Values:APPINSIGHTS_INSTRUMENTATIONKEY"];
                            if (!String.IsNullOrEmpty(appInsightsInstrumentationKey))
                            {
                                loggingBuilder.AddApplicationInsightsWebJobs((opts) => { opts.InstrumentationKey = appInsightsInstrumentationKey; });
                            }
                        })
                    .ConfigureServices((serviceCollection) =>
                        {
                        })
                    .UseConsoleLifetime();

            Console.WriteLine($"Building host.");

            IHost host = builder.Build();

            Console.WriteLine($"Starting host.");

            using (host)
            {
                await host.RunAsync();
            }

            Console.WriteLine($"Host finished.");
            Console.WriteLine($"Press enter to end.");

            Console.ReadLine();
        }
    }
}

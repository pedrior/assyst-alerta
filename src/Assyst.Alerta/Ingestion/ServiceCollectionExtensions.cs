using Assyst.Alerta.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.CircuitBreaker;

namespace Assyst.Alerta.Ingestion;

internal static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddIngestion(IConfiguration configuration)
        {
            services.RegisterIngestionOptions(configuration);
            services.RegisterAssystApiClient();
            services.RegisterCoreServices();

            return services;
        }

        private void RegisterCoreServices()
        {
            var channel = Channel.CreateUnbounded<IReadOnlyList<Event>>(new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = true
            });

            services.AddSingleton(channel.Writer);
            services.AddSingleton(channel.Reader);

            services.AddSingleton<AssystEndpointBuilder>();
            services.AddHostedService<EventIngestionService>();
        }

        private void RegisterAssystApiClient()
        {
            services.AddHttpClient(HttpClientNames.Assyst, (sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<EventIngestionOptions>>().Value;

                    // Polly handles request timing out.
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    
                    // Assyst REST also speaks JSON.
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("Authorization", options.Authorization);
                })
                .AddResilienceHandler("assyst-api-pipeline", (pipeline, context) =>
                {
                    var options = context.ServiceProvider
                        .GetRequiredService<IOptions<EventIngestionOptions>>()
                        .Value;

                    // No retry strategy: producer uses a PeriodicTimer that schedules the next attempt.
                    // Adding retries here would compete with that cadence and queue duplicate work.
                    pipeline.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
                    {
                        FailureRatio = 0.5,
                        MinimumThroughput = 5,
                        BreakDuration = TimeSpan.FromSeconds(30),
                        SamplingDuration = TimeSpan.FromSeconds(30)
                    });

                    pipeline.AddTimeout(options.RequestTimeout);
                });
        }

        private void RegisterIngestionOptions(IConfiguration configuration)
        {
            services.AddOptionsWithValidateOnStart<EventIngestionOptions>()
                .Bind(configuration.GetSection("Ingestion"))
                .ValidateDataAnnotations();
        }
    }
}

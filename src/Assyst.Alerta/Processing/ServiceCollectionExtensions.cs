using Assyst.Alerta.Models;
using Assyst.Alerta.Processing.Evaluators;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Assyst.Alerta.Processing;

internal static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddProcessing(IConfiguration configuration)
        {
            services.RegisterProcessingOptions(configuration);
            services.RegisterCoreServices();

            return services;
        }

        private void RegisterProcessingOptions(IConfiguration configuration)
        {
            services.AddOptionsWithValidateOnStart<EventProcessingOptions>()
                .Bind(configuration.GetSection("Processing"))
                .ValidateDataAnnotations();
        }

        private void RegisterCoreServices()
        {
            var channel = Channel.CreateUnbounded<IReadOnlyList<EventAlert>>(new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = true
            });

            services.AddSingleton(channel.Writer);
            services.AddSingleton(channel.Reader);

            services.AddMemoryCache();

            services.AddSingleton<CallbackFilter>();
            services.AddSingleton<IEventEvaluator, SlaBreachEvaluator>();
            services.AddSingleton<IEventEvaluator, EventReopenEvaluator>();

            services.AddHostedService<EventProcessingService>();
        }
    }
}
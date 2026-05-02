using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Assyst.Alerta.Scheduling;

internal static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddScheduling(IConfiguration configuration)
        {
            services.RegisterSchedulerOptions(configuration);
            services.RegisterCoreServices();

            return services;
        }

        private void RegisterSchedulerOptions(IConfiguration configuration)
        {
            services.AddOptionsWithValidateOnStart<SchedulerOptions>()
                .Bind(configuration.GetSection("Scheduler"))
                .ValidateDataAnnotations()
                .Validate(
                    o => o.StartTime < o.EndTime,
                    $"Scheduler {nameof(SchedulerOptions.StartTime)} must be earlier than {nameof(SchedulerOptions.EndTime)}.");
        }

        private void RegisterCoreServices()
        {
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<Scheduler>();
        }
    }
}
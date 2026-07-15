using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Assyst.Alerta.Notification;

internal static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddNotification(IConfiguration configuration)
        {
            services.RegisterNotificationOptions(configuration);
            services.RegisterCoreServices();

            return services;
        }

        private void RegisterNotificationOptions(IConfiguration configuration)
        {
            services.AddOptionsWithValidateOnStart<EventNotificationOptions>()
                .Bind(configuration.GetSection("Notification"))
                .ValidateDataAnnotations()
                .Validate(
                    o => o.EventUrlFormat.OriginalString.Contains("{0}", StringComparison.Ordinal),
                    $"{nameof(EventNotificationOptions.EventUrlFormat)} must contain a '{{0}}' placeholder for the event ID.")
                .Validate(
                    o => o.Webhooks.Count > 0,
                    $"At least one {nameof(EventNotificationOptions.Webhooks)} entry must be configured.")
                .Validate(
                    o => o.Webhooks.All(w => w.Url is { IsAbsoluteUri: true }),
                    $"Every {nameof(EventNotificationOptions.Webhooks)} entry must have a valid absolute {nameof(WebhookTarget.Url)}.");
        }

        private void RegisterCoreServices()
        {
            services.AddSingleton<AlertDeduplicator>();
            services.AddSingleton<GoogleChatCardBuilder>();
            services.AddSingleton<GoogleChatNotificationDispatcher>();

            services.AddHostedService<EventNotificationService>();
        }
    }
}
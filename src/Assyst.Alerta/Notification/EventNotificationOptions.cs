namespace Assyst.Alerta.Notification;

internal sealed class EventNotificationOptions
{
    [Required, Url]
    public required Uri WebhookUrl { get; init; }

    [Required, Url]
    public required Uri EventUrlFormat { get; init; }
}

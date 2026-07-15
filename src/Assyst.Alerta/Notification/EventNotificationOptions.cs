using Assyst.Alerta.Models;

namespace Assyst.Alerta.Notification;

internal sealed class EventNotificationOptions
{
    [Required, Url]
    public required Uri EventUrlFormat { get; init; }

    [Required, MinLength(1)]
    public required IReadOnlyList<WebhookTarget> Webhooks { get; init; }
}

internal sealed class WebhookTarget
{
    [Required, Url]
    public required Uri Url { get; init; }

    // Empty means the webhook receives alerts for all departments.
    public IReadOnlyList<Department> Departments { get; init; } = [];

    // Empty means the webhook receives all alert types.
    public IReadOnlyList<AlertType> AlertTypes { get; init; } = [];

    public bool Matches(EventAlert alert) =>
        (Departments.Count is 0 || Departments.Contains(alert.Department)) &&
        (AlertTypes.Count is 0 || AlertTypes.Contains(alert.Type));
}

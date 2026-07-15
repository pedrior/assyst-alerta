using Assyst.Alerta.Models;

namespace Assyst.Alerta.Notification;

internal sealed class GoogleChatCardBuilder(IOptions<EventNotificationOptions> options)
{
    public object Build(IReadOnlyList<EventAlert> alerts, DateTimeOffset now)
    {
        var cards = new List<object>();

        // Separate alerts into SLA violations and Reopened events
        var slaAlerts = alerts.Where(a => a.Type is AlertType.Breached or AlertType.NearBreach).ToList();
        var reopenedAlerts = alerts.Where(a => a.Type is AlertType.Reopened).ToList();

        // Build the SLA card if there are any SLA alerts
        if (slaAlerts.Count > 0)
        {
            cards.Add(BuildCard("🚨 Violação de SLA", slaAlerts, now));
        }

        // Build the Reopened card if there are any Reopened alerts
        if (reopenedAlerts.Count > 0)
        {
            cards.Add(BuildCard("🔄 Reabertura de chamado", reopenedAlerts, now, true));
        }

        return new
        {
            cardsV2 = cards.ToArray()
        };
    }

    private object BuildCard(
        string title,
        IReadOnlyList<EventAlert> alerts,
        DateTimeOffset now,
        bool isReopened = false)
    {
        var sections = new object[alerts.Count];
        for (var i = 0; i < alerts.Count; i++)
        {
            sections[i] = BuildSection(alerts[i], now, isReopened);
        }

        return new
        {
            cardId = Guid.NewGuid().ToString("N"),
            card = new
            {
                header = new { title },
                sections
            }
        };
    }

    private object BuildSection(EventAlert alert, DateTimeOffset now, bool isReopened = false)
    {
        var referenceAt = alert.Type is AlertType.Reopened
            ? alert.ReopenedAt ?? throw new InvalidOperationException("Reopened alerts must have ReopenedAt set.")
            : alert.AssignedAt;

        var elapsed = now - referenceAt;
        var elapsedText = FormatDuration(elapsed);

        var (color, statusIcon) = alert.Type switch
        {
            AlertType.Breached => ("#D32F2F", "timer_off"),
            AlertType.NearBreach => ("#E67C00", "schedule"),
            AlertType.Reopened => ("#1A73E8", "restore"),
            _ => throw new ArgumentOutOfRangeException(nameof(alert.Type), alert.Type, "Unsupported alert type")
        };

        var userLabel = alert.IsVipUser ? "Usuário(a) VIP ⚠️" : "Usuário(a)";
        var eventUrl = string.Format(options.Value.EventUrlFormat.OriginalString, alert.Id);

        var widgets = new List<object>
        {
            DecoratedText(
                icon: statusIcon,
                text: isReopened
                    ? $"<b>{alert.Ref}</b>" +
                      $" · {alert.AssignedDeptName}" +
                      $" · {referenceAt:dd/MM/yy HH:mm}"
                    : $"<font color=\"{color}\"><b>{elapsedText}</b></font>" +
                      $" · <b>{alert.Ref}</b>" +
                      $" · {alert.AssignedDeptName}" +
                      $" · {referenceAt:dd/MM/yy HH:mm}",
                button: new
                {
                    text = "Abrir",
                    icon = MaterialIcon("open_in_new"),
                    onClick = new
                    {
                        openLink = new
                        {
                            url = eventUrl
                        }
                    }
                }),
            DecoratedText(
                icon: "person",
                topLabel: userLabel,
                text: alert.UserName),
        };

        if (alert.Type is AlertType.Reopened)
        {
            widgets.Insert(1, DecoratedText(
                icon: "person_check",
                topLabel: "Técnico",
                text: alert.AssignedUser));
        }

        widgets.Add(DecoratedText(
            icon: "description",
            topLabel: "Resumo",
            text: alert.Summary));

        return new
        {
            collapsible = true,
            uncollapsibleWidgetsCount = 3,
            widgets = widgets.ToArray()
        };
    }

    private static object DecoratedText(string icon, string text, string? topLabel = null, object? button = null)
    {
        return new
        {
            decoratedText = new
            {
                startIcon = MaterialIcon(icon),
                topLabel,
                text,
                button
            }
        };
    }

    private static object MaterialIcon(string name) => new
    {
        materialIcon = new
        {
            name
        }
    };

    private static string FormatDuration(TimeSpan timeSpan)
    {
        return timeSpan.TotalHours >= 1
            ? $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes:D2}min"
            : $"{timeSpan.Minutes}min {timeSpan.Seconds:D2}s";
    }
}
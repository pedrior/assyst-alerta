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
            cards.Add(BuildCard("🔄 Reabertura de chamados", reopenedAlerts, now));
        }

        return new
        {
            cardsV2 = cards.ToArray()
        };
    }

    private object BuildCard(string title, IReadOnlyList<EventAlert> alerts, DateTimeOffset now)
    {
        var sections = new object[alerts.Count];
        for (var i = 0; i < alerts.Count; i++)
        {
            sections[i] = BuildSection(alerts[i], now);
        }

        return new
        {
            cardId = Guid.NewGuid().ToString("N"),
            card = new
            {
                header = new
                {
                    title,
                    subtitle = alerts.Count is 1
                        ? "1 chamado requer atenção"
                        : $"{alerts.Count} chamados requerem atenção"
                },
                sections
            }
        };
    }

    private object BuildSection(EventAlert alert, DateTimeOffset now)
    {
        // Reopened alerts measure elapsed time from when the ticket was reopened, not when it was assigned.
        // ReopenedAt is guaranteed to be set whenever Type is Reopened.
        var referenceAt = alert.Type is AlertType.Reopened
            ? alert.ReopenedAt ?? throw new InvalidOperationException("Reopened alerts must have ReopenedAt set.")
            : alert.AssignedAt;

        var elapsed = now - referenceAt;
        var elapsedText = FormatDuration(elapsed);

        var (color, statusIcon, reasonText) = alert.Type switch
        {
            AlertType.Breached => (
                "#D32F2F",
                "timer_off",
                $"Sem técnico atribuído ou pendente de atendimento há {elapsedText}"),
            AlertType.NearBreach => (
                "#E67C00",
                "schedule",
                $"Sem técnico atribuído ou pendente de atendimento há {elapsedText}"),
            AlertType.Reopened => (
                "#1A73E8",
                "restore",
                $"Chamado reaberto em {referenceAt:dd/MM/yy 'às' HH:mm}"),
            _ => throw new ArgumentOutOfRangeException(nameof(alert.Type), alert.Type, "Unsupported alert type")
        };

        var userLabel = alert.IsVipUser ? "Usuário(a) VIP" : "Usuário(a)";
        var eventUrl = string.Format(options.Value.EventUrlFormat.OriginalString, alert.Id);

        return new
        {
            collapsible = true,
            uncollapsibleWidgetsCount = 1,
            widgets = new[]
            {
                // Header: colored elapsed time + reference + department + reference date, with "Abrir" button
                DecoratedText(
                    icon: statusIcon,
                    text: $"<font color=\"{color}\"><b>{elapsedText}</b></font>" +
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
                    icon: "info",
                    topLabel: "Motivo",
                    text: reasonText),
                DecoratedText(icon: "person", topLabel: userLabel, text: alert.UserName),
                DecoratedText(icon: "description", topLabel: "Resumo", text: alert.Summary)
            }
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
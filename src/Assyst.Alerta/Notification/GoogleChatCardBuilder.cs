using Assyst.Alerta.Models;

namespace Assyst.Alerta.Notification;

internal sealed class GoogleChatCardBuilder(IOptions<EventNotificationOptions> options)
{
    public object Build(IReadOnlyList<EventAlert> alerts, DateTimeOffset now)
    {
        var sections = new object[alerts.Count];
        for (var i = 0; i < alerts.Count; i++)
        {
            sections[i] = BuildSection(alerts[i], now);
        }

        return new
        {
            cardsV2 = new[]
            {
                new
                {
                    cardId = Guid.NewGuid().ToString("N"),
                    card = new
                    {
                        header = new
                        {
                            title = "🚨 Alerta",
                            subtitle = alerts.Count is 1
                                ? "1 chamado requer atenção"
                                : $"{alerts.Count} chamados requerem atenção"
                        },
                        sections
                    }
                }
            }
        };
    }

    private object BuildSection(EventAlert alert, DateTimeOffset now)
    {
        var elapsed = now - alert.AssignedAt;
        var elapsedText = FormatDuration(elapsed);

        var isBreached = alert.Type is AlertType.Breached;
        var color = isBreached ? "#D32F2F" : "#E67C00";
        var statusIcon = isBreached ? "timer_off" : "schedule";
        var userLabel = alert.IsVipUser ? "Usuário(a) VIP" : "Usuário(a)";
        var eventUrl = string.Format(options.Value.EventUrlFormat.OriginalString, alert.Id);

        return new
        {
            collapsible = true,
            uncollapsibleWidgetsCount = 1,
            widgets = new[]
            {
                // Header: colored elapsed time + reference + department + assigned date, with "Abrir" button
                DecoratedText(
                    icon: statusIcon,
                    text: $"<font color=\"{color}\"><b>{elapsedText}</b></font>" +
                          $" · <b>{alert.Ref}</b>" +
                          $" · {alert.AssignedDeptName}" +
                          $" · {alert.AssignedAt:dd/MM/yy HH:mm}",
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
                    text: $"Sem técnico atribuído ou pendente de atendimento há {elapsedText}"),
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
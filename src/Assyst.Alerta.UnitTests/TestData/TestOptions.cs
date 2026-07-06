using Assyst.Alerta.Notification;
using Assyst.Alerta.Processing;
using Assyst.Alerta.Scheduling;

namespace Assyst.Alerta.UnitTests.TestData;

internal static class TestOptions
{
    public static IOptions<SchedulerOptions> Scheduler(
        TimeOnly? startTime = null,
        TimeOnly? endTime = null,
        DayOfWeek[]? days = null,
        DateOnly[]? holidays = null)
    {
        return Options.Create(new SchedulerOptions
        {
            StartTime = startTime ?? new TimeOnly(7, 0, 0),
            EndTime = endTime ?? new TimeOnly(19, 0, 0),
            Days = days ??
            [
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday
            ],
            Holidays = holidays ?? []
        });
    }

    public static IOptions<EventProcessingOptions> Processing(
        TimeSpan? sla = null,
        double nearBreachThreshold = 0.75)
    {
        return Options.Create(new EventProcessingOptions
        {
            Sla = sla ?? TimeSpan.FromMinutes(10),
            NearBreachThreshold = nearBreachThreshold
        });
    }

    public static IOptions<EventNotificationOptions> Notification(
        string webhookUrl = "https://chat.googleapis.com/v1/spaces/XXXXX/messages?key=XXXXX&token=XXXXX",
        string eventUrlFormat = "https://assyst.example.com/events/{0}")
    {
        return Options.Create(new EventNotificationOptions
        {
            WebhookUrl = new Uri(webhookUrl, UriKind.Absolute),
            EventUrlFormat = new Uri(eventUrlFormat, UriKind.Absolute)
        });
    }
}
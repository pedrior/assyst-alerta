using Assyst.Alerta.Ingestion;
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

    public static IOptions<EventIngestionOptions> Ingestion(
        string baseUrl = "https://assyst.example.com/",
        string authorization = "Basic dXNlcjpwYXNz",
        int[]? departmentIds = null)
    {
        return Options.Create(new EventIngestionOptions
        {
            BaseUrl = new Uri(baseUrl, UriKind.Absolute),
            Authorization = authorization,
            DepartmentIds = departmentIds ??
            [
                547, // 2N – João Pessoa
                553, // 2N – Patos
                554, // 2N – Souza
                555, // 2N – Campina Grande
                570, // 2N – Manut. Equip.
                594 // 2N – PJe
            ]
        });
    }

    public static IOptions<EventProcessingOptions> Processing(
        TimeSpan? sla = null,
        double nearBreachThreshold = 0.75,
        string[]? assignorDepartmentsFilter = null)
    {
        return Options.Create(new EventProcessingOptions
        {
            Sla = sla ?? TimeSpan.FromMinutes(10),
            NearBreachThreshold = nearBreachThreshold,
            AssignorDepartmentsFilter = assignorDepartmentsFilter ??
            [
                "1_NIVEL"
            ]
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
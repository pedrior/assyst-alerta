using Assyst.Alerta.Models;

namespace Assyst.Alerta.Ingestion;

internal sealed class AssystEndpointBuilder(IOptions<EventIngestionOptions> options)
{
    private const string BasePath = "/assystREST/v2";
    private const string EventsPath = $"{BasePath}/events";

    private static readonly string FieldsQuery = string.Join(
        ",",
        "id",
        "formattedReference",
        "affectedUserId",
        "shortDescription",
        "alertStatusEnum",
        "dateOfLastAssignment",
        "assignedServDeptId",
        "lastSlaClockStop",
        "actions[dateActioned,actioningServDept[name],actionType[shortCode]]");

    public Uri BuildEventsEndpoint(IReadOnlyCollection<Department> departments)
    {
        ArgumentOutOfRangeException.ThrowIfZero(departments.Count);

        var departmentIdQueries = new string[departments.Count];
        var i = 0;
        foreach (var department in departments)
        {
            departmentIdQueries[i++] = $"assignedServDeptId={(int)department}";
        }

        var builder = new UriBuilder(options.Value.BaseUrl)
        {
            Path = EventsPath,
            Query = $"{string.Join('&', departmentIdQueries)}" +
                    $"&eventStatus=open" +
                    $"&fields=[{FieldsQuery}]"
        };

        return builder.Uri;
    }
}
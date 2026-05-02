namespace Assyst.Alerta.Ingestion;

internal sealed class AssystEndpointBuilder(IOptions<EventIngestionOptions> options)
{
    private const string BasePath = "/assystREST/v2";
    private const string EventsPath = $"{BasePath}/events";

    private static readonly string FieldsQuery = string.Join(
        ",",
        "id",
        "affectedUserName",
        "alertStatusEnum",
        "shortDescription",
        "formattedReference",
        "assignedServDeptId",
        "dateOfLastAssignment",
        "lastSlaClockStop",
        "originalAssignedServDeptSC",
        "lastActionTypeId",
        "lastActionServDeptSC");

    public Uri BuildNonAssignedOpenEventsEndpoint(int departmentId)
    {
        var builder = new UriBuilder(options.Value.BaseUrl)
        {
            Path = EventsPath,
            Query = $"assignedServDeptId={departmentId}" +
                    $"&assignedUserId=0" +
                    $"&eventStatus=open" +
                    $"&fields={FieldsQuery}"
        };

        return builder.Uri;
    }
}
using System.Text.Json.Serialization;

namespace Assyst.Alerta.Models;

internal sealed record Event
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("formattedReference")]
    public string Ref { get; init; } = string.Empty;

    [JsonPropertyName("affectedUserName")]
    public string UserName { get; init; } = string.Empty;

    [JsonPropertyName("shortDescription")]
    public string Summary { get; init; } = string.Empty;

    [JsonPropertyName("alertStatusEnum")]
    public string AlertStatus { get; init; } = string.Empty;

    [JsonPropertyName("dateOfLastAssignment")]
    public DateTimeOffset AssignedAt { get; init; }
    
    [JsonPropertyName("assignedUser")]
    public required AssignedUser AssignedUser { get; init; }

    [JsonPropertyName("assignedServDeptId")]
    public required Department AssignedDepartment { get; init; }
    
    [JsonPropertyName("lastSlaClockStop")]
    public DateTimeOffset? SlaClockStoppedAt { get; init; }
    
    [JsonPropertyName("actions")]
    public IReadOnlyCollection<EventAction> Actions { get; init; } = [];
}
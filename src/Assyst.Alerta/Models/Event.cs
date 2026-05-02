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

    [JsonPropertyName("assignedServDeptId")]
    public required int AssignedDeptId { get; init; }

    [JsonPropertyName("originalAssignedServDeptSC")]
    public string OriginDeptName { get; init; } = string.Empty;

    [JsonPropertyName("lastActionTypeId")]
    public int LastActionId { get; init; }

    [JsonPropertyName("lastActionServDeptSC")]
    public string LastActionDeptName { get; init; } = string.Empty;

    [JsonPropertyName("lastSlaClockStop")]
    public DateTimeOffset? SlaClockStoppedAt { get; init; }
}
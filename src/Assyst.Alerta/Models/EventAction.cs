using System.Text.Json.Serialization;

namespace Assyst.Alerta.Models;

internal sealed record EventAction
{
    [JsonPropertyName("id")]
    public uint Id { get; init; }
    
    [JsonPropertyName("actionType")]
    public ActionType Type { get; init; } = ActionType.None;
    
    [JsonPropertyName("dateActioned")]
    public DateTimeOffset CreatedAt { get; init; }
}
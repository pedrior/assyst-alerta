using System.Text.Json.Serialization;

namespace Assyst.Alerta.Models;

internal sealed record ActionType
{
    public static readonly ActionType None = new();

    [JsonPropertyName("shortCode")]
    public string Code { get; init; } = string.Empty;
}
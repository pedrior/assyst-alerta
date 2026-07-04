using System.Text.Json.Serialization;

namespace Assyst.Alerta.Models;

internal sealed class Department
{
    public static readonly Department None = new();
    
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}
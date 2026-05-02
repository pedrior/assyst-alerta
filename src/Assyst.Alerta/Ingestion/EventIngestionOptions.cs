namespace Assyst.Alerta.Ingestion;

internal sealed class EventIngestionOptions
{
    [Url]
    public required Uri BaseUrl { get; init; }

    [Required, MaxLength(2048)]
    public required string Authorization { get; init; }

    [Required, Length(1, 20)]
    public required int[] DepartmentIds { get; init; }
    
    [Range(typeof(TimeSpan), "00:00:01", "00:01:00")]
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(5);

    [Range(typeof(TimeSpan), "00:00:01", "00:01:00")]
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(15);
}
namespace Assyst.Alerta.Processing;

internal sealed class EventProcessingOptions
{
    [Required, Range(typeof(TimeSpan), "00:00:00", "6:00:00")]
    public required TimeSpan Sla { get; init; }

    [Required, Range(0.0, 0.9)]
    public required double NearBreachThreshold { get; init; }

    [Required, MinLength(1)]
    public required string[] AssignorDepartmentsFilter { get; init; }
}
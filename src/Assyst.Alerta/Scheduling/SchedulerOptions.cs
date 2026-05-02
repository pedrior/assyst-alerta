namespace Assyst.Alerta.Scheduling;

internal sealed class SchedulerOptions
{
    [Required]
    public required TimeOnly StartTime { get; init; } 

    [Required]
    public required TimeOnly EndTime { get; init; }
    
    public DayOfWeek[] Days { get; init; } =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday
    ];

    public DateOnly[] Holidays { get; init; } = [];
}

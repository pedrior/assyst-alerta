using System.Collections.Frozen;

namespace Assyst.Alerta.Scheduling;

internal sealed class Scheduler(TimeProvider time, IOptions<SchedulerOptions> options)
{
    private readonly SchedulerOptions options = options.Value;

    private readonly FrozenSet<DayOfWeek> days = options.Value.Days.ToFrozenSet();
    private readonly FrozenSet<DateOnly> holidays = options.Value.Holidays.ToFrozenSet();

    public bool IsNowWithinSchedule()
    {
        var local = time.GetLocalNow();
        if (!days.Contains(local.DayOfWeek))
        {
            return false;
        }

        var localDateOnly = DateOnly.FromDateTime(local.DateTime);
        if (holidays.Contains(localDateOnly))
        {
            return false;
        }

        var timeOfDay = TimeOnly.FromTimeSpan(local.TimeOfDay);
        return timeOfDay >= options.StartTime && timeOfDay < options.EndTime;
    }
}

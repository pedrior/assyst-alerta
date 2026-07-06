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
    
    public TimeSpan CalculateElapsedBusinessTime(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start)
        {
            return TimeSpan.Zero;
        }
        
        var elapsed = TimeSpan.Zero;
        var currentDate = DateOnly.FromDateTime(start.DateTime);
        var endDate = DateOnly.FromDateTime(end.DateTime);

        for (; currentDate <= endDate; currentDate = currentDate.AddDays(1))
        {
            if (!days.Contains(currentDate.DayOfWeek) || holidays.Contains(currentDate))
            {
                continue;
            }

            var offset = time.LocalTimeZone.GetUtcOffset(currentDate.ToDateTime(TimeOnly.MinValue));
            var dayStart = new DateTimeOffset(currentDate.ToDateTime(options.StartTime), offset);
            var dayEnd = new DateTimeOffset(currentDate.ToDateTime(options.EndTime), offset);

            var windowStart = dayStart > start ? dayStart : start;
            var windowEnd = dayEnd < end ? dayEnd : end;

            if (windowStart < windowEnd)
            {
                elapsed += windowEnd - windowStart;
            }
        }

        return elapsed;
    }
}

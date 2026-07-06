using Assyst.Alerta.Scheduling;
using Assyst.Alerta.UnitTests.TestData;
using Microsoft.Extensions.Time.Testing;

namespace Assyst.Alerta.UnitTests.Scheduling;

[TestSubject(typeof(Scheduler))]
public sealed class SchedulerTests
{
    private static readonly DateTimeOffset MondayBaseDate = new(2026, 5, 4, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsNowWithinSchedule_WeekdayInsideWindow_ReturnsTrue()
    {
        // Arrange
        var time = new FakeTimeProvider(MondayBaseDate.AddHours(10));
        var scheduler = new Scheduler(time, TestOptions.Scheduler());

        // Act
        var result = scheduler.IsNowWithinSchedule();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsNowWithinSchedule_AtStartTimeExactly_ReturnsTrue()
    {
        // Arrange
        var time = new FakeTimeProvider(MondayBaseDate.AddHours(9));
        var scheduler = new Scheduler(time, TestOptions.Scheduler());

        // Act
        var result = scheduler.IsNowWithinSchedule();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsNowWithinSchedule_BeforeStartTime_ReturnsFalse()
    {
        // Arrange
        var time = new FakeTimeProvider(MondayBaseDate.AddHours(6).AddMinutes(59));
        var scheduler = new Scheduler(time, TestOptions.Scheduler());

        // Act
        var result = scheduler.IsNowWithinSchedule();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsNowWithinSchedule_AtEndTimeExactly_ReturnsFalse()
    {
        // Arrange
        var time = new FakeTimeProvider(MondayBaseDate.AddHours(19));
        var scheduler = new Scheduler(time, TestOptions.Scheduler());

        // Act
        var result = scheduler.IsNowWithinSchedule();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsNowWithinSchedule_OneSecondBeforeEndTime_ReturnsTrue()
    {
        // Arrange
        var time = new FakeTimeProvider(MondayBaseDate.AddHours(17).AddMinutes(59).AddSeconds(59));
        var scheduler = new Scheduler(time, TestOptions.Scheduler());

        // Act
        var result = scheduler.IsNowWithinSchedule();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsNowWithinSchedule_OnConfiguredHoliday_ReturnsFalse()
    {
        // Arrange
        var holiday = new DateOnly(2026, 5, 4);
        var time = new FakeTimeProvider(MondayBaseDate.AddHours(10));
        var scheduler = new Scheduler(time, TestOptions.Scheduler(holidays: [holiday]));

        // Act
        var result = scheduler.IsNowWithinSchedule();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsNowWithinSchedule_DayNotInConfiguredDays_ReturnsFalse()
    {
        // Arrange
        var time = new FakeTimeProvider(MondayBaseDate.AddHours(10));
        var scheduler = new Scheduler(time, TestOptions.Scheduler(days: [DayOfWeek.Tuesday]));

        // Act
        var result = scheduler.IsNowWithinSchedule();

        // Assert
        result.Should().BeFalse();
    }

    // --- CalculateElapsedBusinessTime -----------------------------------------------

    [Fact]
    public void CalculateElapsedBusinessTime_SameDayWithinWindow_ReturnsWallClockDifference()
    {
        // Arrange
        var time = new FakeTimeProvider(MondayBaseDate.AddHours(10));
        var scheduler = new Scheduler(time, TestOptions.Scheduler());

        var start = MondayBaseDate.AddHours(10); // 10:00
        var end = MondayBaseDate.AddHours(10).AddMinutes(30); // 10:30

        // Act
        var elapsed = scheduler.CalculateElapsedBusinessTime(start, end);

        // Assert
        elapsed.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void CalculateElapsedBusinessTime_StartBeforeWindow_ClampsToWindowStart()
    {
        // Arrange: window is 07:00-19:00, start is 06:00, end is 09:15.
        var time = new FakeTimeProvider(MondayBaseDate.AddHours(9));
        var scheduler = new Scheduler(time, TestOptions.Scheduler());

        var start = MondayBaseDate.AddHours(6);
        var end = MondayBaseDate.AddHours(9).AddMinutes(15);

        // Act
        var elapsed = scheduler.CalculateElapsedBusinessTime(start, end);

        // Assert: clamped to 07:00-09:15 = 2h15min.
        elapsed.Should().Be(TimeSpan.FromHours(2) + TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void CalculateElapsedBusinessTime_EndAfterWindow_ClampsToWindowEnd()
    {
        // Arrange: window is 07:00-19:00, start is 17:45, end is 20:00 (after close).
        var time = new FakeTimeProvider(MondayBaseDate.AddHours(17));
        var scheduler = new Scheduler(time, TestOptions.Scheduler());

        var start = MondayBaseDate.AddHours(17).AddMinutes(45);
        var end = MondayBaseDate.AddHours(20);

        // Act
        var elapsed = scheduler.CalculateElapsedBusinessTime(start, end);

        // Assert: clamped to 17:45-19:00 = 1h15min.
        elapsed.Should().Be(TimeSpan.FromHours(1) + TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void CalculateElapsedBusinessTime_SpansWeekend_ExcludesWeekendTime()
    {
        // Arrange: Friday 17:00 to the following Monday 09:30, window is 07:00-19:00.
        // Expected: 2h (Fri 17:00-19:00) + 2h30min (Mon 07:00-09:30) = 4h30min.
        var friday = MondayBaseDate.AddDays(-3); // 2026-05-01
        var time = new FakeTimeProvider(friday.AddHours(17));
        var scheduler = new Scheduler(time, TestOptions.Scheduler());

        var start = friday.AddHours(17);
        var end = MondayBaseDate.AddHours(9).AddMinutes(30);

        // Act
        var elapsed = scheduler.CalculateElapsedBusinessTime(start, end);

        // Assert
        elapsed.Should().Be(TimeSpan.FromHours(4) + TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void CalculateElapsedBusinessTime_SpansConfiguredHolidays_ExcludesHolidayTime()
    {
        // Arrange: Monday 17:00 to Thursday 09:05, with Tuesday and Wednesday
        // configured as holidays. Window is 07:00-19:00.
        // Expected: 2h (Mon 17:00-19:00) + 2h05min (Thu 07:00-09:05) = 4h05min.
        var time = new FakeTimeProvider(MondayBaseDate.AddHours(17));
        var scheduler = new Scheduler(
            time,
            TestOptions.Scheduler(holidays: [new DateOnly(2026, 5, 5), new DateOnly(2026, 5, 6)]));

        var start = MondayBaseDate.AddHours(17);
        var end = MondayBaseDate.AddDays(3).AddHours(9).AddMinutes(5); // Thursday

        // Act
        var elapsed = scheduler.CalculateElapsedBusinessTime(start, end);

        // Assert
        elapsed.Should().Be(TimeSpan.FromHours(4) + TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void CalculateElapsedBusinessTime_EntirelyOutsideConfiguredDays_ReturnsZero()
    {
        // Arrange: start and end both fall on a Saturday, which isn't a configured day.
        var saturday = MondayBaseDate.AddDays(-2); // 2026-05-02
        var time = new FakeTimeProvider(saturday.AddHours(10));
        var scheduler = new Scheduler(time, TestOptions.Scheduler());

        var start = saturday.AddHours(10);
        var end = saturday.AddHours(14);

        // Act
        var elapsed = scheduler.CalculateElapsedBusinessTime(start, end);

        // Assert
        elapsed.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void CalculateElapsedBusinessTime_WhenEndIsBeforeStart_ReturnsZero()
    {
        // Arrange
        var time = new FakeTimeProvider(MondayBaseDate.AddHours(10));
        var scheduler = new Scheduler(time, TestOptions.Scheduler());

        var start = MondayBaseDate.AddHours(10);
        var end = MondayBaseDate.AddHours(9);

        // Act
        var elapsed = scheduler.CalculateElapsedBusinessTime(start, end);

        // Assert
        elapsed.Should().Be(TimeSpan.Zero);
    }
}
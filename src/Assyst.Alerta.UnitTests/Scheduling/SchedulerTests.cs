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
}
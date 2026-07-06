using Assyst.Alerta.Models;
using Assyst.Alerta.Processing.Evaluators;
using Assyst.Alerta.Scheduling;
using Assyst.Alerta.UnitTests.TestData;
using Microsoft.Extensions.Time.Testing;

namespace Assyst.Alerta.UnitTests.Processing;

[TestSubject(typeof(SlaBreachEvaluator))]
public sealed class SlaBreachEvaluatorTests
{
    private static readonly DateTimeOffset AssignedAt = EventBuilder.DefaultAssignedAt;

    // Used by tests that only exercise SLA threshold math, not calendar awareness.
    private static readonly IOptions<SchedulerOptions> AlwaysOpen = Options.Create(new SchedulerOptions
    {
        StartTime = TimeOnly.MinValue,
        EndTime = TimeOnly.MaxValue,
        Days =
        [
            DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday
        ],
        Holidays = []
    });

    private static SlaBreachEvaluator NewEvaluator(
        DateTimeOffset? now = null,
        TimeSpan? sla = null,
        double nearBreachThreshold = 0.75,
        IOptions<SchedulerOptions>? schedulerOptions = null)
    {
        var time = new FakeTimeProvider();
        time.SetUtcNow(now ?? DateTimeOffset.UtcNow);

        var options = TestOptions.Processing(sla, nearBreachThreshold);
        var scheduler = new Scheduler(time, schedulerOptions ?? AlwaysOpen);

        return new SlaBreachEvaluator(time, scheduler, options, NullLogger<SlaBreachEvaluator>.Instance);
    }

    [Fact]
    public void Evaluate_WhenSlaClockPaused_ReturnsNull()
    {
        // Arrange
        var evaluator = NewEvaluator(now: AssignedAt.AddMinutes(11));
        var @event = new EventBuilder()
            .WithPausedAt(AssignedAt.AddMinutes(15))
            .Build();

        // Act
        var alert = evaluator.Evaluate(@event);

        // Assert
        alert.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(7)] // The threshold is 75% of 10 minutes.
    public void Evaluate_WhenProgressBelowNearBreachThreshold_ReturnsNull(int elapsedMinutes)
    {
        // Arrange
        var evaluator = NewEvaluator(now: AssignedAt.AddMinutes(elapsedMinutes));
        var @event = new EventBuilder().Build();

        // Act
        var result = evaluator.Evaluate(@event);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    public void Evaluate_WhenProgressBetweenNearBreachAndFullSla_ReturnsNearBreach(int elapsedMinutes)
    {
        // Arrange
        var evaluator = NewEvaluator(now: AssignedAt.AddMinutes(elapsedMinutes));
        var @event = new EventBuilder().Build();

        // Act
        var alert = evaluator.Evaluate(@event);

        // Assert
        alert.Should().NotBeNull();
        alert.Type.Should().Be(AlertType.NearBreach);
    }

    [Theory]
    [InlineData(240)]
    [InlineData(360)]
    [InlineData(600)]
    public void Evaluate_WhenProgressAtOrAboveFullSla_ReturnsBreached(int elapsedMinutes)
    {
        // Arrange
        var evaluator = NewEvaluator(now: AssignedAt.AddMinutes(elapsedMinutes));
        var @event = new EventBuilder().Build();

        // Act
        var alert = evaluator.Evaluate(@event);

        // Assert
        alert.Should().NotBeNull();
        alert.Type.Should().Be(AlertType.Breached);
    }

    [Theory]
    [InlineData("RED", true)]
    [InlineData("NORMAL", false)]
    [InlineData("", false)]
    public void Evaluate_AlertVipFlag_TrackedFromAlertStatus(string alertStatus, bool expectedVip)
    {
        // Arrange
        var evaluator = NewEvaluator(now: AssignedAt.AddMinutes(11));
        var @event = new EventBuilder().WithAlertStatus(alertStatus).Build();

        // Act
        var alert = evaluator.Evaluate(@event);

        // Assert
        alert.Should().NotBeNull();
        alert.IsVipUser.Should().Be(expectedVip);
    }
    
    [Fact]
    public void Evaluate_AssignedAtIsTruncatedToSeconds()
    {
        // This attempts to mimic the behavior of the IFS Assyst web portal, which
        // truncates the assignedAt field to seconds.

        // Arrange
        var assignedAtWithMillis = new DateTimeOffset(2026, 5, 4, 10, 0, 0, 999, TimeSpan.Zero);
        var evaluator = NewEvaluator(now: assignedAtWithMillis.AddHours(5));
        var @event = new EventBuilder()
            .WithAssignedAt(assignedAtWithMillis)
            .Build();

        // Act
        var alert = evaluator.Evaluate(@event);

        // Assert
        alert.Should().NotBeNull();
        alert.AssignedAt.Millisecond.Should().Be(0);
        alert.AssignedAt.Should().Be(new DateTimeOffset(2026, 5, 4, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Evaluate_PropagatesEventScalarFields()
    {
        // Arrange
        var evaluator = NewEvaluator(now: AssignedAt.AddMinutes(11));
        var @event = new EventBuilder()
            .WithId(645167)
            .WithRef("S1456789")
            .WithSummary("PJe com lentidão")
            .WithUserName("Ana Carla")
            .Build();

        // Act
        var alert = evaluator.Evaluate(@event);

        // Assert
        alert.Should().NotBeNull();
        alert.Id.Should().Be(645167);
        alert.Ref.Should().Be("S1456789");
        alert.Summary.Should().Be("PJe com lentidão");
        alert.UserName.Should().Be("Ana Carla");
    }

    // --- Business-hours awareness -------------------------------------------------

    [Fact]
    public void Evaluate_AcrossHolidayGap_CountsOnlyBusinessTimeElapsed()
    {
        // Arrange: assigned Monday 17:00 (1h before close). Tuesday and Wednesday
        // are holidays. "Now" is Thursday 09:05. Wall-clock elapsed is ~64h, but
        // business elapsed is only 1h (Mon) + 5min (Thu) = 65min, above the 10min SLA.
        var assignedAt = new DateTimeOffset(2026, 5, 4, 17, 0, 0, TimeSpan.Zero); // Monday
        var now = new DateTimeOffset(2026, 5, 7, 9, 5, 0, TimeSpan.Zero); // Thursday

        var schedulerOptions = Options.Create(new SchedulerOptions
        {
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(18, 0),
            Holidays = [new DateOnly(2026, 5, 5), new DateOnly(2026, 5, 6)]
        });

        var evaluator = NewEvaluator(now: now, schedulerOptions: schedulerOptions);
        var @event = new EventBuilder().WithAssignedAt(assignedAt).Build();

        // Act
        var alert = evaluator.Evaluate(@event);

        // Assert
        alert.Should().NotBeNull();
        alert.Type.Should().Be(AlertType.Breached);
    }

    [Fact]
    public void Evaluate_AcrossHolidayGap_DoesNotFalselyBreachWhenBusinessTimeIsLow()
    {
        // Arrange: assigned Monday 17:58 (2min before close). Tuesday and Wednesday
        // are holidays. "Now" is Thursday 09:03. Business elapsed is only
        // 2min (Mon) + 3min (Thu) = 5min, below the 7.5min near-breach threshold,
        // even though ~64h have passed on the wall clock.
        var assignedAt = new DateTimeOffset(2026, 5, 4, 17, 58, 0, TimeSpan.Zero); // Monday
        var now = new DateTimeOffset(2026, 5, 7, 9, 3, 0, TimeSpan.Zero); // Thursday

        var schedulerOptions = Options.Create(new SchedulerOptions
        {
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(18, 0),
            Holidays = [new DateOnly(2026, 5, 5), new DateOnly(2026, 5, 6)]
        });

        var evaluator = NewEvaluator(now: now, schedulerOptions: schedulerOptions);
        var @event = new EventBuilder().WithAssignedAt(assignedAt).Build();

        // Act
        var alert = evaluator.Evaluate(@event);

        // Assert
        alert.Should().BeNull();
    }
}
using Assyst.Alerta.Models;
using Assyst.Alerta.Processing.Evaluators;
using Assyst.Alerta.UnitTests.TestData;
using Microsoft.Extensions.Time.Testing;

namespace Assyst.Alerta.UnitTests.Processing;

[TestSubject(typeof(SlaBreachEvaluator))]
public sealed class SlaBreachEvaluatorTests
{
    private static readonly DateTimeOffset AssignedAt = EventBuilder.DefaultAssignedAt;

    private static SlaBreachEvaluator NewEvaluator(
        DateTimeOffset? now = null,
        TimeSpan? sla = null,
        double nearBreachThreshold = 0.75,
        string[]? assignorDepartmentsFilter = null)
    {
        var time = new FakeTimeProvider();
        time.SetUtcNow(now ?? DateTimeOffset.UtcNow);

        var options = TestOptions.Processing(sla, nearBreachThreshold, assignorDepartmentsFilter);

        return new SlaBreachEvaluator(time, options, NullLogger<SlaBreachEvaluator>.Instance);
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
    public void Evaluate_DepartmentResolvedForKnownId()
    {
        // Arrange
        var evaluator = NewEvaluator(now: AssignedAt.AddMinutes(11));
        var @event = new EventBuilder()
            .WithAssignedDepartmentId(553)
            .Build();

        // Act
        var alert = evaluator.Evaluate(@event);

        // Assert
        alert.Should().NotBeNull();
        alert.AssignedDeptName.Should().Be("2N – Patos");
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
}
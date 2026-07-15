using Assyst.Alerta.Models;
using Assyst.Alerta.Notification;
using Assyst.Alerta.UnitTests.TestData;
using Microsoft.Extensions.Caching.Memory;

namespace Assyst.Alerta.UnitTests.Notification;

[TestSubject(typeof(AlertDeduplicator))]
public sealed class AlertDeduplicatorTests
{
    private static AlertDeduplicator NewDeduplicator() => new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public void ShouldNotify_OnFreshCache_NearBreach_ReturnsTrue()
    {
        // Assert
        var deduplicator = NewDeduplicator();
        var alert = new EventAlertBuilder()
            .WithId(1)
            .WithType(AlertType.NearBreach)
            .Build();

        // Act
        var result = deduplicator.ShouldNotify(alert);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldNotify_OnFreshCache_Breached_ReturnsTrue()
    {
        // Assert
        var deduplicator = NewDeduplicator();
        var alert = new EventAlertBuilder()
            .WithId(1)
            .WithType(AlertType.Breached)
            .Build();

        // Act
        var result = deduplicator.ShouldNotify(alert);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldNotify_AfterMarkSameSeverity_ReturnsFalse()
    {
        // Arrange
        var deduplicator = NewDeduplicator();
        var alert = new EventAlertBuilder()
            .WithId(1)
            .WithType(AlertType.NearBreach)
            .Build();

        deduplicator.MarkNotified(alert);

        // Act
        var result = deduplicator.ShouldNotify(alert);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldNotify_UpgradeFromNearBreachToBreached_ReturnsTrue()
    {
        // Arrange
        var deduplicator = NewDeduplicator();
        var nearBreach = new EventAlertBuilder()
            .WithId(1)
            .WithType(AlertType.NearBreach)
            .Build();

        var breached = new EventAlertBuilder()
            .WithId(1)
            .WithType(AlertType.Breached)
            .Build();

        deduplicator.MarkNotified(nearBreach);

        // Act
        var result = deduplicator.ShouldNotify(breached);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldNotify_DowngradeFromBreachedToNearBreach_ReturnsFalse()
    {
        // Arrange
        var deduplicator = NewDeduplicator();
        var breached = new EventAlertBuilder()
            .WithId(1)
            .WithType(AlertType.Breached)
            .Build();

        var nearBreach = new EventAlertBuilder()
            .WithId(1)
            .WithType(AlertType.NearBreach)
            .Build();

        deduplicator.MarkNotified(breached);

        // Act
        var result = deduplicator.ShouldNotify(nearBreach);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldNotify_RepeatedBreachedForSameTicket_ReturnsFalse()
    {
        // Arrange
        var deduplicator = NewDeduplicator();
        var breached = new EventAlertBuilder()
            .WithId(1)
            .WithType(AlertType.Breached)
            .Build();

        deduplicator.MarkNotified(breached);

        // Act
        var result = deduplicator.ShouldNotify(breached);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldNotify_SecondReopenWithDifferentActionId_ReturnsTrue()
    {
        // Arrange
        var deduplicator = NewDeduplicator();
        var firstReopen = new EventAlertBuilder()
            .WithId(1)
            .WithType(AlertType.Reopened)
            .WithActionId(100)
            .Build();

        var secondReopen = new EventAlertBuilder()
            .WithId(1)
            .WithType(AlertType.Reopened)
            .WithActionId(200)
            .Build();

        deduplicator.MarkNotified(firstReopen);

        // Act
        var result = deduplicator.ShouldNotify(secondReopen);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldNotify_SameReopenActionId_ReturnsFalse()
    {
        // Arrange
        var deduplicator = NewDeduplicator();
        var reopen = new EventAlertBuilder()
            .WithId(1)
            .WithType(AlertType.Reopened)
            .WithActionId(100)
            .Build();

        deduplicator.MarkNotified(reopen);

        // Act
        var result = deduplicator.ShouldNotify(reopen);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldNotify_SlaBreachAfterReopen_ForSameTicket_ReturnsTrue()
    {
        // Arrange
        var deduplicator = NewDeduplicator();
        var reopen = new EventAlertBuilder()
            .WithId(1)
            .WithType(AlertType.Reopened)
            .WithActionId(100)
            .Build();

        var breached = new EventAlertBuilder()
            .WithId(1)
            .WithType(AlertType.Breached)
            .Build();

        deduplicator.MarkNotified(reopen);

        // Act
        var result = deduplicator.ShouldNotify(breached);

        // Assert — a reopen must not poison SLA alerts for the same ticket.
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldNotify_DifferentTicketIds_AreIndependent()
    {
        // Arrange
        var deduplicator = NewDeduplicator();
        var alert1 = new EventAlertBuilder()
            .WithId(1)
            .WithType(AlertType.Breached)
            .Build();

        var alert2 = new EventAlertBuilder()
            .WithId(2)
            .WithType(AlertType.NearBreach)
            .Build();

        // Act
        var alert1Result = deduplicator.ShouldNotify(alert1);
        var alert2Result = deduplicator.ShouldNotify(alert2);

        // Assert
        alert1Result.Should().BeTrue();
        alert2Result.Should().BeTrue();
    }
}
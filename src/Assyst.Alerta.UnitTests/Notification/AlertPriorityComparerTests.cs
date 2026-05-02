using Assyst.Alerta.Models;
using Assyst.Alerta.Notification;
using Assyst.Alerta.UnitTests.TestData;

namespace Assyst.Alerta.UnitTests.Notification;

public sealed class AlertPriorityComparerTests
{
    [Fact]
    public void Sort_BreachedSortsBeforeNearBreach()
    {
        // Arrange
        var nearBreach = new EventAlertBuilder()
            .WithId(1)
            .WithType(AlertType.NearBreach)
            .Build();

        var breached = new EventAlertBuilder()
            .WithId(2)
            .WithType(AlertType.Breached)
            .Build();

        EventAlert[] alerts = [nearBreach, breached];

        // Act
        alerts.Sort(AlertPriorityComparer.Instance);

        // Assert
        alerts[0].Should().BeSameAs(breached);
        alerts[1].Should().BeSameAs(nearBreach);
    }

    [Fact]
    public void Sort_WithinSameSeverity_VipSortsBeforeNonVip()
    {
        var nonVip = new EventAlertBuilder()
            .WithId(1)
            .WithType(AlertType.Breached)
            .WithVip(false)
            .Build();

        var vip = new EventAlertBuilder()
            .WithId(2)
            .WithType(AlertType.Breached)
            .WithVip()
            .Build();

        EventAlert[] alerts = [nonVip, vip];

        // Act
        alerts.Sort(AlertPriorityComparer.Instance);

        // Assert
        alerts[0].Should().BeSameAs(vip);
        alerts[1].Should().BeSameAs(nonVip);
    }

    [Fact]
    public void Sort_BreachedNonVipBeforeNearBreachVip()
    {
        var nearBreachVip = new EventAlertBuilder()
            .WithId(1)
            .WithType(AlertType.NearBreach)
            .WithVip()
            .Build();

        var breachedNonVip = new EventAlertBuilder()
            .WithId(2)
            .WithType(AlertType.Breached)
            .WithVip(false)
            .Build();

        EventAlert[] alerts = [nearBreachVip, breachedNonVip];

        // Act
        alerts.Sort(AlertPriorityComparer.Instance);

        // Assert
        alerts[0].Should().BeSameAs(breachedNonVip);
    }

    [Fact]
    public void Compare_SameSeverityAndSameVipStatus_ReturnsZero()
    {
        // Arrange
        var alert1 = new EventAlertBuilder()
            .WithId(1)
            .WithType(AlertType.Breached)
            .WithVip()
            .Build();

        var alert2 = new EventAlertBuilder()
            .WithId(2)
            .WithType(AlertType.Breached)
            .WithVip()
            .Build();

        // Act
        var result = AlertPriorityComparer.Instance.Compare(alert1, alert2);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void Compare_NullInputs_ReturnZero()
    {
        // Arrange
        var alert = new EventAlertBuilder().Build();

        // Act
        var result1 = AlertPriorityComparer.Instance.Compare(null, alert);
        var result2 = AlertPriorityComparer.Instance.Compare(alert, null);
        var result3 = AlertPriorityComparer.Instance.Compare(null, null);

        // Assert
        result1.Should().Be(result2).And.Be(result3).And.Be(0);
    }

    [Fact]
    public void Sort_FullPrecedenceOrder()
    {
        // Arrange
        var breachedVip = new EventAlertBuilder()
            .WithId(1)
            .WithType(AlertType.Breached)
            .WithVip()
            .Build();

        var breachedNonVip = new EventAlertBuilder()
            .WithId(2)
            .WithType(AlertType.Breached)
            .WithVip(false)
            .Build();

        var nearBreachVip = new EventAlertBuilder()
            .WithId(3)
            .WithType(AlertType.NearBreach)
            .WithVip()
            .Build();

        var nearBreachNonVip = new EventAlertBuilder()
            .WithId(4)
            .WithType(AlertType.NearBreach)
            .WithVip(false)
            .Build();

        EventAlert[] alerts = [nearBreachNonVip, nearBreachVip, breachedNonVip, breachedVip];

        // Act
        alerts.Sort(AlertPriorityComparer.Instance);

        // Assert
        alerts[0].Should().BeSameAs(breachedVip);
        alerts[1].Should().BeSameAs(breachedNonVip);
        alerts[2].Should().BeSameAs(nearBreachVip);
        alerts[3].Should().BeSameAs(nearBreachNonVip);
    }
}
using Assyst.Alerta.Models;
using Assyst.Alerta.Processing;
using Microsoft.Extensions.Caching.Memory;

namespace Assyst.Alerta.UnitTests.Processing;

[TestSubject(typeof(CallbackFilter))]
public sealed class CallbackFilterTests
{
    private static CallbackFilter NewFilter() => new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public void IsAlertRegistered_BeforeRegister_ReturnsFalse()
    {
        // Arrange
        var filter = NewFilter();

        // Act
        var result = filter.IsAlertRegistered(1023610, AlertType.Reopened);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAlertRegistered_AfterRegister_ReturnsTrue()
    {
        // Arrange
        var filter = NewFilter();
        filter.RegisterAlert(1023610, AlertType.Reopened);

        // Act
        var result = filter.IsAlertRegistered(1023610, AlertType.Reopened);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAlertRegistered_DifferentIdsAreIsolated()
    {
        // Arrange
        var filter = NewFilter();
        filter.RegisterAlert(1023610, AlertType.Reopened);

        // Act
        var result = filter.IsAlertRegistered(1044789, AlertType.Reopened);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAlertRegistered_DifferentTypesForSameEventAreIsolated()
    {
        // Arrange
        var filter = NewFilter();
        filter.RegisterAlert(1023610, AlertType.NearBreach);

        // Act
        var result = filter.IsAlertRegistered(1023610, AlertType.Breached);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RegisterAlert_IsIdempotent()
    {
        // Arrange
        var filter = NewFilter();

        filter.RegisterAlert(1023610, AlertType.Reopened);
        filter.RegisterAlert(1023610, AlertType.Reopened);

        // Act
        var result = filter.IsAlertRegistered(1023610, AlertType.Reopened);

        // Assert
        result.Should().BeTrue();
    }
}
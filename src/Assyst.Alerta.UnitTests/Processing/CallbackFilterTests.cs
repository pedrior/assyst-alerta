using Assyst.Alerta.Processing;
using Microsoft.Extensions.Caching.Memory;

namespace Assyst.Alerta.UnitTests.Processing;

[TestSubject(typeof(CallbackFilter))]
public sealed class CallbackFilterTests
{
    private static CallbackFilter NewFilter() => new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public void IsEventRegistered_BeforeRegister_ReturnsFalse()
    {
        // Arrange
        var filter = NewFilter();

        // Act
        var result = filter.IsEventRegistered(1023610);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsEventRegistered_AfterRegister_ReturnsTrue()
    {
        // Arrange
        var filter = NewFilter();
        filter.RegisterEvent(1023610);

        // Act
        var result = filter.IsEventRegistered(1023610);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsEventRegistered_DifferentIdsAreIsolated()
    {
        // Arrange
        var filter = NewFilter();
        filter.RegisterEvent(1023610);

        // Act
        var result = filter.IsEventRegistered(1044789);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RegisterEvent_IsIdempotent()
    {
        // Arrange
        var filter = NewFilter();

        filter.RegisterEvent(1023610);
        filter.RegisterEvent(1023610);

        // Act
        var result = filter.IsEventRegistered(1023610);

        // Assert
        result.Should().BeTrue();
    }
}
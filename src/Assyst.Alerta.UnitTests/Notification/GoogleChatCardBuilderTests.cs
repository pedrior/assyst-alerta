using System.Text.Json;
using System.Text.Json.Nodes;
using Assyst.Alerta.Models;
using Assyst.Alerta.Notification;
using Assyst.Alerta.UnitTests.TestData;

namespace Assyst.Alerta.UnitTests.Notification;

[TestSubject(typeof(GoogleChatCardBuilder))]
public sealed class GoogleChatCardBuilderTests
{
    private static readonly DateTimeOffset AssignedAt = EventAlertBuilder.DefaultAssignedAt;

    private static GoogleChatCardBuilder NewBuilder(string eventUrlFormat = "https://assyst.example.com/event/{0}")
    {
        return new GoogleChatCardBuilder(TestOptions.Notification(eventUrlFormat: eventUrlFormat));
    }

    private static JsonNode BuildAndParse(
        GoogleChatCardBuilder builder,
        IReadOnlyList<EventAlert> alerts,
        DateTimeOffset now)
    {
        return JsonNode.Parse(JsonSerializer.Serialize(builder.Build(alerts, now)))!;
    }

    [Fact]
    public void Build_SingleAlert_HasSingularSubtitle()
    {
        // Arrange
        var alerts = new[]
        {
            new EventAlertBuilder().Build()
        };

        // Act
        var node = BuildAndParse(NewBuilder(), alerts, AssignedAt.AddMinutes(10));

        // Assert
        node["cardsV2"]![0]!["card"]!["header"]!["subtitle"]!
            .GetValue<string>()
            .Should().Be("1 chamado requer atenção");
    }

    [Fact]
    public void Build_MultipleAlerts_HasPluralSubtitleAndOneSectionPerAlert()
    {
        // Arrange
        var alerts = new[]
        {
            new EventAlertBuilder().WithId(1).Build(),
            new EventAlertBuilder().WithId(2).Build(),
            new EventAlertBuilder().WithId(3).Build()
        };

        // Act
        var node = BuildAndParse(NewBuilder(), alerts, AssignedAt.AddMinutes(10));
        var card = node["cardsV2"]![0]!["card"]!;

        card["header"]!["subtitle"]!
            .GetValue<string>()
            .Should().Be("3 chamados requerem atenção");

        card["sections"]!
            .AsArray().Should().HaveCount(3);
    }

    [Theory]
    [InlineData(true, "Usuário(a) VIP")]
    [InlineData(false, "Usuário(a)")]
    public void Build_UserWidget_LabelReflectsVipFlag(bool isVip, string expectedLabel)
    {
        // Arrange
        var alerts = new[]
        {
            new EventAlertBuilder()
                .WithVip(isVip)
                .Build()
        };

        // Act
        var node = BuildAndParse(NewBuilder(), alerts, AssignedAt.AddHours(1));

        // Assert
        var userWidget = node["cardsV2"]![0]!["card"]!["sections"]![0]!["widgets"]![2]!["decoratedText"]!;

        userWidget["topLabel"]!
            .GetValue<string>()
            .Should().Be(expectedLabel);
    }

    [Fact]
    public void Build_HeaderWidgetButton_LinksToFormattedEventUrl()
    {
        // Arrange
        var alerts = new[]
        {
            new EventAlertBuilder().WithId(42).Build()
        };

        // Act
        var node = BuildAndParse(
            NewBuilder(),
            alerts,
            AssignedAt.AddHours(1));

        // Assert
        node["cardsV2"]![0]!["card"]!["sections"]![0]!["widgets"]![0]!
            ["decoratedText"]!["button"]!["onClick"]!["openLink"]!["url"]!
            .GetValue<string>()
            .Should().Be("https://assyst.example.com/event/42");
    }

    [Theory]
    [InlineData(0, 30, "0min 30s")]
    [InlineData(5, 12, "5min 12s")]
    [InlineData(63, 0, "1h 03min")]
    [InlineData(165, 0, "2h 45min")]
    public void Build_HeaderText_ContainsFormattedElapsedDuration(
        int elapsedMinutes,
        int elapsedSeconds,
        string expected)
    {
        // Arrange
        var alerts = new[]
        {
            new EventAlertBuilder().Build()
        };

        var now = AssignedAt.AddMinutes(elapsedMinutes).AddSeconds(elapsedSeconds);

        // Act
        var node = BuildAndParse(NewBuilder(), alerts, now);

        // Assert
        node["cardsV2"]![0]!["card"]!["sections"]![0]!["widgets"]![0]!
            ["decoratedText"]!["text"]!.GetValue<string>()
            .Should().Contain($"<b>{expected}</b>");
    }
}
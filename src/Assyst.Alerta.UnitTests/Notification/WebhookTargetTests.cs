using Assyst.Alerta.Models;
using Assyst.Alerta.Notification;
using Assyst.Alerta.UnitTests.TestData;

namespace Assyst.Alerta.UnitTests.Notification;

[TestSubject(typeof(WebhookTarget))]
public sealed class WebhookTargetTests
{
    private static WebhookTarget Target(
        IReadOnlyList<Department>? departments = null,
        IReadOnlyList<AlertType>? alertTypes = null) => new()
    {
        Url = new Uri("https://chat.googleapis.com/v1/spaces/XXXXX/messages", UriKind.Absolute),
        Departments = departments ?? [],
        AlertTypes = alertTypes ?? []
    };

    [Fact]
    public void Matches_EmptyFilters_MatchesEverything()
    {
        var target = Target();
        var alert = new EventAlertBuilder()
            .WithType(AlertType.Breached)
            .WithDepartmentId(Department.N3Seguranca)
            .Build();

        target.Matches(alert).Should().BeTrue();
    }

    [Fact]
    public void Matches_DepartmentInFilter_ReturnsTrue()
    {
        var target = Target(departments: [Department.N2JoaoPessoa, Department.N2CampinaGrande]);
        var alert = new EventAlertBuilder()
            .WithDepartmentId(Department.N2CampinaGrande)
            .Build();

        target.Matches(alert).Should().BeTrue();
    }

    [Fact]
    public void Matches_DepartmentNotInFilter_ReturnsFalse()
    {
        var target = Target(departments: [Department.N2JoaoPessoa]);
        var alert = new EventAlertBuilder()
            .WithDepartmentId(Department.N2Patos)
            .Build();

        target.Matches(alert).Should().BeFalse();
    }

    [Fact]
    public void Matches_AlertTypeInFilter_ReturnsTrue()
    {
        var target = Target(alertTypes: [AlertType.Reopened]);
        var alert = new EventAlertBuilder()
            .WithType(AlertType.Reopened)
            .WithActionId(1)
            .Build();

        target.Matches(alert).Should().BeTrue();
    }

    [Fact]
    public void Matches_AlertTypeNotInFilter_ReturnsFalse()
    {
        var target = Target(alertTypes: [AlertType.Reopened]);
        var alert = new EventAlertBuilder()
            .WithType(AlertType.Breached)
            .Build();

        target.Matches(alert).Should().BeFalse();
    }

    [Fact]
    public void Matches_CombinedFilters_RequireBoth()
    {
        var target = Target(
            departments: [Department.N2JoaoPessoa],
            alertTypes: [AlertType.Breached]);

        var matching = new EventAlertBuilder()
            .WithType(AlertType.Breached)
            .WithDepartmentId(Department.N2JoaoPessoa)
            .Build();

        var wrongType = new EventAlertBuilder()
            .WithType(AlertType.NearBreach)
            .WithDepartmentId(Department.N2JoaoPessoa)
            .Build();

        var wrongDept = new EventAlertBuilder()
            .WithType(AlertType.Breached)
            .WithDepartmentId(Department.N2Patos)
            .Build();

        target.Matches(matching).Should().BeTrue();
        target.Matches(wrongType).Should().BeFalse();
        target.Matches(wrongDept).Should().BeFalse();
    }
}

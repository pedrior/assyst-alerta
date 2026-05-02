using Assyst.Alerta.Models;

namespace Assyst.Alerta.UnitTests.TestData;

internal sealed class EventAlertBuilder
{
    public static readonly DateTimeOffset DefaultAssignedAt = new(2026, 5, 4, 10, 0, 0, TimeSpan.Zero);

    private AlertType type = AlertType.NearBreach;
    private int id = 6605525;
    private string @ref = "S1605525";
    private string summary = "Intranet com problemas";
    private string userName = "Thayna Maria Araujo Martins";
    private bool isVipUser;
    private string department = "2N – João Pessoa";
    private DateTimeOffset assignedAt = DefaultAssignedAt;

    public EventAlertBuilder WithType(AlertType value)
    {
        type = value;
        return this;
    }

    public EventAlertBuilder WithId(int value)
    {
        id = value;
        return this;
    }

    public EventAlertBuilder WithRef(string value)
    {
        @ref = value;
        return this;
    }

    public EventAlertBuilder WithSummary(string value)
    {
        summary = value;
        return this;
    }

    public EventAlertBuilder WithUserName(string value)
    {
        userName = value;
        return this;
    }

    public EventAlertBuilder WithVip(bool value = true)
    {
        isVipUser = value;
        return this;
    }

    public EventAlertBuilder WithDepartment(string value)
    {
        department = value;
        return this;
    }

    public EventAlertBuilder WithAssignedAt(DateTimeOffset value)
    {
        assignedAt = value;
        return this;
    }

    public EventAlert Build() => new()
    {
        Type = type,
        Id = id,
        Ref = @ref,
        Summary = summary,
        UserName = userName,
        IsVipUser = isVipUser,
        AssignedDeptName = department,
        AssignedAt = assignedAt
    };
}
using Assyst.Alerta.Models;
using Assyst.Alerta.Processing;

namespace Assyst.Alerta.UnitTests.TestData;

internal sealed class EventBuilder
{
    public static readonly DateTimeOffset DefaultAssignedAt = new(2026, 5, 4, 10, 0, 0, TimeSpan.Zero);

    private int id = 6605525;
    private string @ref = "S1605525";
    private string userName = "Thayna Maria Araujo Martins";
    private string summary = "Estou precisando de um dispositivo que tenha grande capacidade de memória";
    private string alertStatus = string.Empty;
    private DateTimeOffset assignedAt = DefaultAssignedAt;
    private int assignedDepartmentId = 547;
    private string originDepartmentName = "1_NIVEL";
    private int lastActionTypeId = EventActions.Assigned;
    private string lastActionDepartmentName = "1_NIVEL";
    private DateTimeOffset? pausedAt;

    public EventBuilder WithId(int value)
    {
        id = value;
        return this;
    }

    public EventBuilder WithRef(string value)
    {
        @ref = value;
        return this;
    }

    public EventBuilder WithUserName(string value)
    {
        userName = value;
        return this;
    }

    public EventBuilder WithSummary(string value)
    {
        summary = value;
        return this;
    }

    public EventBuilder WithAlertStatus(string value)
    {
        alertStatus = value;
        return this;
    }

    public EventBuilder WithAssignedAt(DateTimeOffset value)
    {
        assignedAt = value;
        return this;
    }

    public EventBuilder WithAssignedDepartmentId(int value)
    {
        assignedDepartmentId = value;
        return this;
    }

    public EventBuilder WithOriginDepartmentName(string value)
    {
        originDepartmentName = value;
        return this;
    }

    public EventBuilder WithLastActionTypeId(int value)
    {
        lastActionTypeId = value;
        return this;
    }

    public EventBuilder WithLastActionDepartmentName(string value)
    {
        lastActionDepartmentName = value;
        return this;
    }

    public EventBuilder WithPausedAt(DateTimeOffset? value)
    {
        pausedAt = value;
        return this;
    }

    public Event Build() => new()
    {
        Id = id,
        Ref = @ref,
        UserName = userName,
        Summary = summary,
        AlertStatus = alertStatus,
        AssignedAt = assignedAt,
        AssignedDeptId = assignedDepartmentId,
        OriginDeptName = originDepartmentName,
        LastActionId = lastActionTypeId,
        LastActionDeptName = lastActionDepartmentName,
        SlaClockStoppedAt = pausedAt
    };
}
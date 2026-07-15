using Assyst.Alerta.Models;

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
    private Department assignedDepartment = Department.N2JoaoPessoa;
    private AssignedUser assignedUser = new() { Name = "Ricardo Souza" };
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

    public EventBuilder WithAssignedDepartmentId(Department value)
    {
        assignedDepartment = value;
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
        AssignedDepartment = assignedDepartment,
        SlaClockStoppedAt = pausedAt,
        AssignedUser = assignedUser
    };
}
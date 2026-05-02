namespace Assyst.Alerta.Models;

internal sealed record EventAlert
{
    public required AlertType Type { get; init; }

    public required int Id { get; init; }

    public required string Ref { get; init; }

    public required string Summary { get; init; }

    public required string UserName { get; init; }

    public required bool IsVipUser { get; init; }

    public required string AssignedDeptName { get; init; }

    public required DateTimeOffset AssignedAt { get; init; }
}
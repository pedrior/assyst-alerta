using System.Collections.Frozen;

namespace Assyst.Alerta.Processing;

internal static class EventDepartments
{
    private static readonly FrozenDictionary<int, string> WellKnownDepartments = new Dictionary<int, string>
    {
        [547] = "2N – João Pessoa",
        [553] = "2N – Patos",
        [554] = "2N – Souza",
        [555] = "2N – Campina Grande",
        [570] = "2N – Manut. Equip.",
        [594] = "2N – PJe"
    }.ToFrozenDictionary();

    public static string GetName(int departmentId)
    {
        return WellKnownDepartments.TryGetValue(departmentId, out var name) ? name : $"{departmentId}";
    }
}
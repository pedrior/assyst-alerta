using Assyst.Alerta.Models;

namespace Assyst.Alerta.Notification;

internal sealed class AlertPriorityComparer : IComparer<EventAlert>
{
    public static readonly AlertPriorityComparer Instance = new();

    public int Compare(EventAlert? a, EventAlert? b)
    {
        if (a is null || b is null)
        {
            return 0;
        }

        // Breached (higher ordinal) first, then VIP first.
        var severity = b.Type.CompareTo(a.Type);
        return severity is not 0
            ? severity
            : b.IsVipUser.CompareTo(a.IsVipUser);
    }
}

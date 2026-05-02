using Assyst.Alerta.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Assyst.Alerta.Notification;

internal sealed class AlertDeduplicator(IMemoryCache cache)
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(6);

    public bool ShouldNotify(EventAlert alert)
    {
        if (cache.TryGetValue(FormatKey(alert.Id), out AlertType previous) && previous >= alert.Type)
        {
            return false;
        }

        return true;
    }

    public void MarkNotified(EventAlert alert)
    {
        cache.Set(FormatKey(alert.Id), alert.Type, Ttl);
    }

    private static string FormatKey(int ticketId) => $"dedup:{ticketId}";
}

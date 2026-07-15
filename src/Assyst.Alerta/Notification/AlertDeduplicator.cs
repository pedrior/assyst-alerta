using Assyst.Alerta.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Assyst.Alerta.Notification;

internal sealed class AlertDeduplicator(IMemoryCache cache)
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(6);

    public bool ShouldNotify(EventAlert alert)
    {
        // Reopens are keyed per reopen action, so a distinct reopen is always a fresh key:
        // presence alone means this exact reopen was already notified.
        if (alert.Type is AlertType.Reopened)
        {
            return !cache.TryGetValue(FormatKey(alert), out _);
        }

        if (cache.TryGetValue(FormatKey(alert), out AlertType previous) && previous >= alert.Type)
        {
            return false;
        }

        return true;
    }

    public void MarkNotified(EventAlert alert)
    {
        cache.Set(FormatKey(alert), alert.Type, Ttl);
    }

    private static string FormatKey(EventAlert alert) => alert.Type is AlertType.Reopened
        ? $"dedup:{alert.Id}:reopen:{alert.ActionId}"
        : $"dedup:{alert.Id}";
}

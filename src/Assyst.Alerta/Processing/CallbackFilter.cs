using Assyst.Alerta.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Assyst.Alerta.Processing;

internal sealed class CallbackFilter(IMemoryCache cache)
{
    private readonly TimeSpan ttl = TimeSpan.FromHours(6);

    public bool IsAlertRegistered(int eventId, AlertType type, long? actionId = null) => 
        cache.TryGetValue(FormatKey(eventId, type, actionId), out _);

    public void RegisterAlert(int eventId, AlertType type, long? actionId = null) => 
        cache.Set(FormatKey(eventId, type, actionId), true, ttl);

    private static string FormatKey(int eventId, AlertType type, long? actionId) => 
        actionId.HasValue ? $"cb:{eventId}:{type}:{actionId}" : $"cb:{eventId}:{type}";
}
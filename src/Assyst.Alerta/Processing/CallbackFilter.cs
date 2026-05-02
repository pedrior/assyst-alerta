using Microsoft.Extensions.Caching.Memory;

namespace Assyst.Alerta.Processing;

internal sealed class CallbackFilter(IMemoryCache cache)
{
    private readonly TimeSpan ttl = TimeSpan.FromHours(6);

    public bool IsEventRegistered(int id) => cache.TryGetValue(FormatEventKey(id), out _);

    public void RegisterEvent(int id) => cache.Set(FormatEventKey(id), true, ttl);

    private static string FormatEventKey(int id) => $"cb:{id}";
}
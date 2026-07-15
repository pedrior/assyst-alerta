using Assyst.Alerta.Models;
using Microsoft.Extensions.Hosting;

namespace Assyst.Alerta.Notification;

internal sealed partial class EventNotificationService(
    TimeProvider time,
    AlertDeduplicator deduplicator,
    GoogleChatCardBuilder cardBuilder,
    ChannelReader<IReadOnlyList<EventAlert>> alertsReader,
    GoogleChatNotificationDispatcher notificationDispatcher,
    IOptions<EventNotificationOptions> options,
    ILogger<EventNotificationService> logger) : BackgroundService
{
    private const int MaxSectionsPerCard = 10;

    protected override async Task ExecuteAsync(CancellationToken cancellation)
    {
        try
        {
            await foreach (var batch in alertsReader.ReadAllAsync(cancellation))
            {
                LogBatchReceived(batch.Count);

                var pending = FilterNewAlerts(batch);
                if (pending.Count is 0)
                {
                    LogAllAlertsDeduplicated(batch.Count);
                    continue;
                }

                // Sort alerts by severity (breached first) then by VIP.
                pending.Sort(AlertPriorityComparer.Instance);

                await RouteAsync(pending, time.GetLocalNow(), cancellation);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogUnexpectedError(ex);
        }
    }

    private List<EventAlert> FilterNewAlerts(IReadOnlyList<EventAlert> batch)
    {
        // Collapse in-batch SLA alerts for the same ticket to the most severe, but keep each
        // distinct reopen (keyed by its action) so SLA and reopen alerts never merge — they may
        // route to different webhooks.
        var collapsed = new Dictionary<string, EventAlert>(batch.Count);
        foreach (var alert in batch)
        {
            var key = alert.Type is AlertType.Reopened
                ? $"reopen:{alert.Id}:{alert.ActionId}"
                : $"sla:{alert.Id}";

            if (!collapsed.TryGetValue(key, out var existing) || alert.Type > existing.Type)
            {
                collapsed[key] = alert;
            }
        }

        var pending = new List<EventAlert>(collapsed.Count);
        foreach (var alert in collapsed.Values)
        {
            if (deduplicator.ShouldNotify(alert))
            {
                pending.Add(alert);
            }
            else
            {
                LogAlertDeduplicated(alert.Ref, alert.Type);
            }
        }

        return pending;
    }

    private async Task RouteAsync(List<EventAlert> alerts, DateTimeOffset now, CancellationToken cancellation)
    {
        // An alert fans out to every webhook whose filters match it. Mark each alert notified
        // once, after routing, if it reached at least one webhook successfully.
        var notified = new HashSet<EventAlert>();

        foreach (var target in options.Value.Webhooks)
        {
            var matching = alerts.Where(target.Matches).ToList();
            if (matching.Count is 0)
            {
                continue;
            }

            foreach (var chunk in matching.Chunk(MaxSectionsPerCard))
            {
                try
                {
                    var payload = cardBuilder.Build(chunk, now);
                    var dispatched = await notificationDispatcher.DispatchAsync(
                        target.Url, payload, chunk.Length, cancellation);
                    if (!dispatched)
                    {
                        continue;
                    }

                    foreach (var alert in chunk)
                    {
                        notified.Add(alert);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogChunkDispatchFailed(ex, chunk.Length);
                }
            }
        }

        foreach (var alert in notified)
        {
            deduplicator.MarkNotified(alert);
        }
    }

    [LoggerMessage(LogLevel.Debug, "Received batch of {Count} alert(s)")]
    partial void LogBatchReceived(int count);

    [LoggerMessage(LogLevel.Debug, "Alert for {Ref} ({Type}) deduplicated, skipping notification")]
    partial void LogAlertDeduplicated(string @ref, AlertType type);

    [LoggerMessage(LogLevel.Debug, "All {Count} alert(s) in batch were deduplicated")]
    partial void LogAllAlertsDeduplicated(int count);

    [LoggerMessage(LogLevel.Error, "Failed to dispatch chunk of {Count} alert(s), continuing with next chunk")]
    partial void LogChunkDispatchFailed(Exception ex, int count);

    [LoggerMessage(LogLevel.Error, "Event notification service encountered an unexpected error")]
    partial void LogUnexpectedError(Exception ex);
}
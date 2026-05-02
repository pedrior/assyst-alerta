using Assyst.Alerta.Models;
using Microsoft.Extensions.Hosting;

namespace Assyst.Alerta.Notification;

internal sealed partial class EventNotificationService(
    TimeProvider time,
    AlertDeduplicator deduplicator,
    GoogleChatCardBuilder cardBuilder,
    ChannelReader<IReadOnlyList<EventAlert>> alertsReader,
    GoogleChatNotificationDispatcher notificationDispatcher,
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

                await DispatchInChunksAsync(pending, time.GetLocalNow(), cancellation);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogUnexpectedError(ex);
        }
    }

    private List<EventAlert> FilterNewAlerts(IReadOnlyList<EventAlert> batch)
    {
        // Collapse multiple in-batch alerts for the same ticket to the most severe.
        var collapsed = new Dictionary<int, EventAlert>(batch.Count);
        foreach (var alert in batch)
        {
            if (!collapsed.TryGetValue(alert.Id, out var existing) || alert.Type > existing.Type)
            {
                collapsed[alert.Id] = alert;
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

    private async Task DispatchInChunksAsync(List<EventAlert> alerts, DateTimeOffset now, CancellationToken cancellation)
    {
        foreach (var chunk in alerts.Chunk(MaxSectionsPerCard))
        {
            try
            {
                var payload = cardBuilder.Build(chunk, now);
                var dispatched = await notificationDispatcher.DispatchAsync(payload, chunk.Length, cancellation);
                if (!dispatched)
                {
                    continue;
                }

                foreach (var alert in chunk)
                {
                    deduplicator.MarkNotified(alert);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogChunkDispatchFailed(ex, chunk.Length);
            }
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
using Assyst.Alerta.Models;
using Microsoft.Extensions.Hosting;

namespace Assyst.Alerta.Processing;

internal sealed partial class EventProcessingService(
    TimeProvider time,
    ISlaEvaluator slaEvaluator,
    CallbackFilter callbackFilter,
    ChannelReader<IReadOnlyList<Event>> eventsReader,
    ChannelWriter<IReadOnlyList<EventAlert>> alertsWriter,
    ILogger<EventProcessingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellation)
    {
        LogServiceStarted();

        try
        {
            await foreach (var events in eventsReader.ReadAllAsync(cancellation))
            {
                LogProcessingBatch(events.Count, events[0].AssignedDeptId);

                var now = time.GetLocalNow();
                var alerts = new List<EventAlert>();

                foreach (var @event in events)
                {
                    if (callbackFilter.IsEventRegistered(@event.Id))
                    {
                        LogEventSkippedSeenCallback(@event.Ref);

                        continue;
                    }

                    if (slaEvaluator.Evaluate(@event, now) is { } alert)
                    {
                        alerts.Add(alert);
                    }
                }

                if (alerts.Count > 0)
                {
                    await alertsWriter.WriteAsync(alerts, cancellation);

                    LogProducedAlerts(alerts.Count);
                }
            }
        }
        finally
        {
            alertsWriter.TryComplete();

            LogServiceStopped();
        }
    }

    [LoggerMessage(LogLevel.Debug, "Event processing service started")]
    partial void LogServiceStarted();

    [LoggerMessage(LogLevel.Debug, "Event processing service stopped")]
    partial void LogServiceStopped();

    [LoggerMessage(LogLevel.Debug, "Processing batch of {Count} event(s) from department {DeptId}")]
    partial void LogProcessingBatch(int count, int deptId);

    [LoggerMessage(LogLevel.Debug, "Event {Ref} skipped, callback already registered")]
    partial void LogEventSkippedSeenCallback(string @ref);

    [LoggerMessage(LogLevel.Information, "Produced {Count} alert(s)")]
    partial void LogProducedAlerts(int count);
}
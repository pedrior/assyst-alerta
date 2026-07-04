using Assyst.Alerta.Models;
using Assyst.Alerta.Processing.Evaluators;
using Microsoft.Extensions.Hosting;

namespace Assyst.Alerta.Processing;

internal sealed partial class EventProcessingService(
    IEnumerable<IEventEvaluator> evaluators,
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
                LogProcessingBatch(events.Count);

                var alerts = new List<EventAlert>();

                foreach (var @event in events)
                {
                    foreach (var evaluator in evaluators)
                    {
                        if (evaluator.Evaluate(@event) is not { } alert)
                        {
                            continue;
                        }

                        // Pass the ActionId to uniquely identify this specific occurrence
                        if (callbackFilter.IsAlertRegistered(@event.Id, alert.Type, alert.ActionId))
                        {
                            LogAlertSkippedSeenCallback(@event.Ref, alert.Type);
                            continue;
                        }

                        callbackFilter.RegisterAlert(@event.Id, alert.Type, alert.ActionId);
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

    [LoggerMessage(LogLevel.Debug, "Processing batch of {Count} event(s)")]
    partial void LogProcessingBatch(int count);

    [LoggerMessage(LogLevel.Debug, "Event {Ref} skipped, {Type} callback already registered")]
    partial void LogAlertSkippedSeenCallback(string @ref, AlertType type);

    [LoggerMessage(LogLevel.Information, "Produced {Count} alert(s)")]
    partial void LogProducedAlerts(int count);
}
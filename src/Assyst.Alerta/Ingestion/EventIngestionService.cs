using Assyst.Alerta.Models;
using Assyst.Alerta.Scheduling;
using Microsoft.Extensions.Hosting;

namespace Assyst.Alerta.Ingestion;

internal sealed partial class EventIngestionService(
    Scheduler scheduler,
    AssystEndpointBuilder endpointBuilder,
    ChannelWriter<IReadOnlyList<Event>> eventsWriter,
    IHttpClientFactory httpClientFactory,
    IOptions<EventIngestionOptions> options,
    ILoggerFactory loggerFactory,
    ILogger<EventIngestionService> logger) : BackgroundService
{
    private readonly EventIngestionOptions options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken cancellation)
    {
        LogServiceStarted();

        var producers = SpawnProducers(cancellation);

        try
        {
            await Task.WhenAll(producers);
        }
        catch (OperationCanceledException)
        {
            // Expected on graceful shutdown.
        }
        catch (Exception ex)
        {
            LogEventProducerExitedUnexpectedly(ex);

            eventsWriter.Complete(ex);
        }
        finally
        {
            eventsWriter.Complete();

            LogServiceStopped();
        }
    }

    private Task[] SpawnProducers(CancellationToken cancellation)
    {
        var tasks = new Task[options.DepartmentIds.Length];
        for (var i = 0; i < options.DepartmentIds.Length; i++)
        {
            var departmentId = options.DepartmentIds[i];
            var producer = new EventProducer(
                departmentId,
                endpointBuilder.BuildNonAssignedOpenEventsEndpoint(departmentId),
                scheduler,
                eventsWriter,
                httpClientFactory.CreateClient(HttpClientNames.Assyst),
                options.PollingInterval,
                loggerFactory.CreateLogger<EventProducer>());

            tasks[i] = producer.RunAsync(cancellation);
        }

        LogSpawnedEventProducers(tasks.Length);

        return tasks;
    }

    [LoggerMessage(LogLevel.Debug, "Event ingestion service started")]
    partial void LogServiceStarted();

    [LoggerMessage(LogLevel.Debug, "Event ingestion service stopped")]
    partial void LogServiceStopped();

    [LoggerMessage(LogLevel.Information, "Spawned {Count} event producer(s)")]
    partial void LogSpawnedEventProducers(int count);

    [LoggerMessage(LogLevel.Error, "One or more event producers exited unexpectedly")]
    partial void LogEventProducerExitedUnexpectedly(Exception ex);
}
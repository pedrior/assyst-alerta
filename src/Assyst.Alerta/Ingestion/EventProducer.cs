using System.Net.Http.Json;
using Assyst.Alerta.Models;
using Assyst.Alerta.Scheduling;

namespace Assyst.Alerta.Ingestion;

internal sealed partial class EventProducer(
    int deptId,
    Uri endpoint,
    Scheduler scheduler,
    ChannelWriter<IReadOnlyList<Event>> eventsWriter,
    HttpClient httpClient,
    TimeSpan pollingInterval,
    ILogger<EventProducer> logger)
{
    private bool? wasWithinSchedule;

    public async Task RunAsync(CancellationToken cancellation)
    {
        LogProducerStarted(deptId, pollingInterval);

        using var timer = new PeriodicTimer(pollingInterval);

        do
        {
            var isWithinSchedule = scheduler.IsNowWithinSchedule();
            if (isWithinSchedule != wasWithinSchedule)
            {
                if (isWithinSchedule)
                {
                    LogInsideSchedule(deptId);
                }
                else
                {
                    LogOutsideSchedule(deptId);
                }

                wasWithinSchedule = isWithinSchedule;
            }

            if (isWithinSchedule)
            {
                await FetchAndPublishAsync(cancellation);
            }
        } while (await timer.WaitForNextTickAsync(cancellation));
    }

    private async Task FetchAndPublishAsync(CancellationToken cancellation)
    {
        try
        {
            using var response = await httpClient.GetAsync(
                endpoint,
                HttpCompletionOption.ResponseHeadersRead,
                cancellation);

            response.EnsureSuccessStatusCode();

            var events = await response.Content.ReadFromJsonAsync<IReadOnlyList<Event>>(cancellation);
            if (events is { Count: > 0 })
            {
                await eventsWriter.WriteAsync(events, cancellation);

                LogEventsPublished(events.Count, deptId);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown path.
            throw;
        }
        catch (HttpRequestException ex)
        {
            // Log and continue; a transient failure shouldn't kill the producer task.
            LogFetchFailed(ex, deptId);
        }
        catch (Exception ex)
        {
            LogFetchUnexpectedError(ex, deptId);
        }
    }

    [LoggerMessage(LogLevel.Information, "Producer started polling from department {DeptId} every {Interval}")]
    partial void LogProducerStarted(int deptId, TimeSpan interval);

    [LoggerMessage(LogLevel.Information, "Producer for department {DeptId} is now inside the active schedule")]
    partial void LogInsideSchedule(int deptId);

    [LoggerMessage(LogLevel.Information, "Producer for department {DeptId} is now outside the active schedule")]
    partial void LogOutsideSchedule(int deptId);

    [LoggerMessage(LogLevel.Information, "Published {Count} event(s) from department {DeptId}")]
    partial void LogEventsPublished(int count, int deptId);

    [LoggerMessage(LogLevel.Warning, "Fetch failed from department {DeptId}")]
    partial void LogFetchFailed(Exception ex, int deptId);

    [LoggerMessage(LogLevel.Error, "Unexpected error fetching from {DeptId}")]
    partial void LogFetchUnexpectedError(Exception ex, int deptId);
}
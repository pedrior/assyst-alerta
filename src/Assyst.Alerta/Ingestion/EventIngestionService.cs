using System.Net.Http.Json;
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
    ILogger<EventIngestionService> logger) : BackgroundService
{
    private static readonly Departments[] MonitoredDepartments = Enum.GetValues<Departments>();

    private readonly EventIngestionOptions options = options.Value;
    private readonly HttpClient httpClient = httpClientFactory.CreateClient(HttpClientNames.Assyst);

    private bool? wasWithinSchedule;

    protected override async Task ExecuteAsync(CancellationToken cancellation)
    {
        var endpoint = endpointBuilder.BuildEventsEndpoint(MonitoredDepartments);

        LogServiceStarted(MonitoredDepartments.Length, options.PollingInterval);

        try
        {
            await PollAsync(endpoint, cancellation);
        }
        catch (OperationCanceledException)
        {
            // Expected on graceful shutdown.
        }
        catch (Exception ex)
        {
            LogPollingLoopFailed(ex);

            eventsWriter.Complete(ex);
        }
        finally
        {
            eventsWriter.Complete();

            LogServiceStopped();
        }
    }

    private async Task PollAsync(Uri endpoint, CancellationToken cancellation)
    {
        using var timer = new PeriodicTimer(options.PollingInterval);

        do
        {
            var isWithinSchedule = scheduler.IsNowWithinSchedule();
            if (isWithinSchedule != wasWithinSchedule)
            {
                if (isWithinSchedule)
                {
                    LogInsideSchedule();
                }
                else
                {
                    LogOutsideSchedule();
                }

                wasWithinSchedule = isWithinSchedule;
            }

            if (isWithinSchedule)
            {
                await FetchAndPublishAsync(endpoint, cancellation);
            }
        } while (await timer.WaitForNextTickAsync(cancellation));
    }

    private async Task FetchAndPublishAsync(Uri endpoint, CancellationToken cancellation)
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

                LogEventsPublished(events.Count);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown path.
            throw;
        }
        catch (HttpRequestException ex)
        {
            // Log and continue; a transient failure shouldn't stop the polling loop.
            LogFetchFailed(ex);
        }
        catch (Exception ex)
        {
            LogFetchUnexpectedError(ex);
        }
    }

    [LoggerMessage(LogLevel.Information, "Event ingestion service started polling {Count} department(s) every {Interval}")]
    partial void LogServiceStarted(int count, TimeSpan interval);

    [LoggerMessage(LogLevel.Debug, "Event ingestion service stopped")]
    partial void LogServiceStopped();

    [LoggerMessage(LogLevel.Information, "Now inside the active schedule")]
    partial void LogInsideSchedule();

    [LoggerMessage(LogLevel.Information, "Now outside the active schedule")]
    partial void LogOutsideSchedule();

    [LoggerMessage(LogLevel.Information, "Published {Count} event(s)")]
    partial void LogEventsPublished(int count);

    [LoggerMessage(LogLevel.Warning, "Fetch failed")]
    partial void LogFetchFailed(Exception ex);

    [LoggerMessage(LogLevel.Error, "Unexpected error during fetch")]
    partial void LogFetchUnexpectedError(Exception ex);

    [LoggerMessage(LogLevel.Error, "Polling loop exited unexpectedly")]
    partial void LogPollingLoopFailed(Exception ex);
}
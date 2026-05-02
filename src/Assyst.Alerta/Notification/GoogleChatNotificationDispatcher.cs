using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Assyst.Alerta.Notification;

internal sealed partial class GoogleChatNotificationDispatcher(
    TimeProvider time,
    IHttpClientFactory httpClientFactory,
    IOptions<EventNotificationOptions> options,
    ILogger<GoogleChatNotificationDispatcher> logger)
{
    private const int MaxRetries = 3;

    // Google Chat Webhooks have a rate limit of 1 request per second.
    private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(500);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private long lastPostTimestamp;

    public async Task<bool> DispatchAsync(object payload, int count, CancellationToken cancellation)
    {
        using var client = httpClientFactory.CreateClient();

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            await ThrottleAsync(cancellation);

            using var response = await client.PostAsJsonAsync(
                options.Value.WebhookUrl,
                payload,
                JsonOptions,
                cancellation);

            lastPostTimestamp = time.GetTimestamp();

            if (response.IsSuccessStatusCode)
            {
                LogNotificationSent(count);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync(cancellation);
            LogNotificationFailed(response.StatusCode, error, attempt, MaxRetries);

            if (attempt < MaxRetries)
            {
                await Task.Delay(RetryDelay * attempt, time, cancellation);
            }
        }

        LogNotificationRetriesExhausted(count);
        return false;
    }

    private async Task ThrottleAsync(CancellationToken cancellation)
    {
        if (lastPostTimestamp is 0)
        {
            return;
        }

        var delay = MinInterval - time.GetElapsedTime(lastPostTimestamp);
        if (delay > TimeSpan.Zero)
        {
            LogThrottlingRequest(delay);
            await Task.Delay(delay, time, cancellation);
        }
    }

    [LoggerMessage(LogLevel.Information, "Google Chat notification sent for {Count} alert(s)")]
    partial void LogNotificationSent(int count);

    [LoggerMessage(
        LogLevel.Warning,
        "Google Chat notification failed: {StatusCode} {Error} (attempt {Attempt}/{MaxAttempts})")]
    partial void LogNotificationFailed(HttpStatusCode statusCode, string error, int attempt, int maxAttempts);

    [LoggerMessage(
        LogLevel.Error,
        "Google Chat notification exhausted all retries for {Count} alert(s), alerts may have been missed")]
    partial void LogNotificationRetriesExhausted(int count);

    [LoggerMessage(LogLevel.Debug, "Throttling webhook request, waiting {Delay:g}")]
    partial void LogThrottlingRequest(TimeSpan delay);
}
using Assyst.Alerta.Models;
using Microsoft.Data.Sqlite;

namespace Assyst.Alerta.Processing;

internal sealed class CallbackFilter
{
    private readonly string connectionString;

    public CallbackFilter(string connectionString)
    {
        this.connectionString = connectionString;
        EnsureSchema();
    }

    public bool IsAlertRegistered(int eventId, AlertType type, long? actionId = null)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM callback_filter WHERE key = $key AND expires_at > $now LIMIT 1;";
        cmd.Parameters.AddWithValue("$key", FormatKey(eventId, type, actionId));
        cmd.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        return cmd.ExecuteScalar() is not null;
    }

    public void RegisterAlert(int eventId, AlertType type, long? actionId = null)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO callback_filter (key, expires_at) VALUES ($key, $expires)
            ON CONFLICT(key) DO UPDATE SET expires_at = excluded.expires_at;

            DELETE FROM callback_filter WHERE expires_at <= $now;
            """;
        
        var ttl = type is AlertType.Reopened ? TimeSpan.FromDays(3) : TimeSpan.FromMinutes(30);
        
        cmd.Parameters.AddWithValue("$key", FormatKey(eventId, type, actionId));
        cmd.Parameters.AddWithValue("$expires", DateTimeOffset.UtcNow.Add(ttl).ToUnixTimeSeconds());
        cmd.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        cmd.ExecuteNonQuery();
    }

    private void EnsureSchema()
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS callback_filter (
                key        TEXT PRIMARY KEY,
                expires_at INTEGER NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private static string FormatKey(int eventId, AlertType type, long? actionId) =>
        actionId.HasValue ? $"cb:{eventId}:{type}:{actionId}" : $"cb:{eventId}:{type}";
}
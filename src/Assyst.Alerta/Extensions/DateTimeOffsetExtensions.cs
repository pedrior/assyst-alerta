namespace Assyst.Alerta.Extensions;

internal static class DateTimeOffsetExtensions
{
    public static DateTimeOffset TruncateToSeconds(this DateTimeOffset value) =>
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Offset);
}
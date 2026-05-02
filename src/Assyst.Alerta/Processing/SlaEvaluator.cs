using Assyst.Alerta.Extensions;
using Assyst.Alerta.Models;

namespace Assyst.Alerta.Processing;

internal sealed partial class SlaEvaluator(IOptions<EventProcessingOptions> options, ILogger<SlaEvaluator> logger)
    : ISlaEvaluator
{
    private const string VipAlertStatus = "RED";

    private readonly EventProcessingOptions options = options.Value;

    public EventAlert? Evaluate(Event @event, DateTimeOffset now)
    {
        // Check if SLA evaluation can be applied.
        if (options.NearBreachThreshold is 0.0 || options.Sla == TimeSpan.Zero)
        {
            return null;
        }

        if (!EvaluateEligibility(@event))
        {
            LogEventIneligibleForSlaEvaluation(@event.Ref);

            return null;
        }

        var assignedAt = @event.AssignedAt
            .TruncateToSeconds();

        var elapsed = now - assignedAt;
        var progress = elapsed / options.Sla;

        if (progress < options.NearBreachThreshold)
        {
            LogEventWithinSla(@event.Ref, elapsed, progress);

            return null;
        }

        if (progress >= 1.0)
        {
            LogEventBreachedSla(@event.Ref, elapsed, progress);

            return CreateAlert(AlertType.Breached, @event);
        }

        LogEventNearingBreachSla(@event.Ref, elapsed, progress);

        return CreateAlert(AlertType.NearBreach, @event);
    }

    private bool EvaluateEligibility(Event @event)
    {
        // 1. Event must not be paused.
        if (@event.SlaClockStoppedAt is not null)
        {
            return false;
        }

        var isAssignorValid = options.AssignorDepartmentsFilter.Contains(@event.LastActionDeptName);

        // The department that created the event is also the department that owns the last action on the event.
        var isOriginSameAsLastAction = @event.OriginDeptName == @event.LastActionDeptName;

        // 2. If the assignor is not whitelisted, the origin department must be the same as the last action department.
        if (!isAssignorValid && !isOriginSameAsLastAction)
        {
            return false;
        }

        // 3. If the event is assigned, it's eligible regardless of the assignor or origin department.
        if (@event.LastActionId is EventActions.Assigned)
        {
            return true;
        }

        // 4.
        // 4.1. If the event's last action is assigned, it's eligible regardless of the assignor or origin department.
        // 4.2. If the event's last action is callback, it's eligible if the assignor is whitelisted and the origin
        // department is the same as the last action department.
        return @event.LastActionId is EventActions.Assigned
               || (@event.LastActionId is EventActions.Callback && isOriginSameAsLastAction && isAssignorValid);
    }

    private static EventAlert CreateAlert(AlertType type, Event @event) => new()
    {
        Type = type,
        Id = @event.Id,
        Ref = @event.Ref,
        Summary = @event.Summary,
        UserName = @event.UserName,
        IsVipUser = @event.AlertStatus is VipAlertStatus,
        AssignedDeptName = EventDepartments.GetName(@event.AssignedDeptId),
        AssignedAt = @event.AssignedAt.TruncateToSeconds()
    };

    [LoggerMessage(LogLevel.Information, "Event {Ref} ineligible for SLA evaluation")]
    partial void LogEventIneligibleForSlaEvaluation(string @ref);

    [LoggerMessage(LogLevel.Debug, "Event {Ref} within SLA alert threshold: {Elapsed} elapsed ({Progress:P})")]
    partial void LogEventWithinSla(string @ref, TimeSpan elapsed, double progress);

    [LoggerMessage(LogLevel.Information, "Event {Ref} nearing SLA breach: {Elapsed} elapsed ({Progress:P})")]
    partial void LogEventNearingBreachSla(string @ref, TimeSpan elapsed, double progress);

    [LoggerMessage(LogLevel.Information, "Event {Ref} breached SLA: {Elapsed} elapsed ({Progress:P})")]
    partial void LogEventBreachedSla(string @ref, TimeSpan elapsed, double progress);
}
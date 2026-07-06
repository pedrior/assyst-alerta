using Assyst.Alerta.Extensions;
using Assyst.Alerta.Models;
using Assyst.Alerta.Scheduling;

namespace Assyst.Alerta.Processing.Evaluators;

internal sealed partial class SlaBreachEvaluator(
    TimeProvider time,
    Scheduler scheduler,
    IOptions<EventProcessingOptions> options,
    ILogger<SlaBreachEvaluator> logger) : IEventEvaluator
{
    private const string VipAlertStatus = "RED";
    
    private static readonly HashSet<Department> AllowedDepartments =
    [
        Department.N2JoaoPessoa,
        Department.N2CampinaGrande,
        Department.N2Patos,
        Department.N2Sousa,
        Department.N2ManutencaoEquipamento,
        Department.N2PJe,
        Department.N2SuporteEspecializado
    ];

    private readonly EventProcessingOptions options = options.Value;

    public EventAlert? Evaluate(Event evt)
    {
        // Check if SLA evaluation can be applied.
        if (options.NearBreachThreshold is 0.0 || options.Sla == TimeSpan.Zero || !EvaluateEligibility(evt))
        {
            return null;
        }

        var assignedAt = evt.AssignedAt.TruncateToSeconds();
        var elapsed = scheduler.CalculateElapsedBusinessTime(assignedAt, time.GetLocalNow());
        var progress = elapsed / options.Sla;

        if (progress < options.NearBreachThreshold)
        {
            LogEventWithinSla(evt.Ref, elapsed, progress);

            return null;
        }

        if (progress >= 1.0)
        {
            LogEventBreachedSla(evt.Ref, elapsed, progress);

            return CreateAlert(AlertType.Breached, evt);
        }

        LogEventNearingBreachSla(evt.Ref, elapsed, progress);

        return CreateAlert(AlertType.NearBreach, evt);
    }

    private static bool EvaluateEligibility(Event evt)
    {
        if (!AllowedDepartments.Contains(evt.AssignedDepartment))
        {
            return false;
        }

        return evt.SlaClockStoppedAt is null && evt.Actions.All(a => a.Type.Code != "EM_ATENDIMENTO_2N");
    }

    private static EventAlert CreateAlert(AlertType type, Event @event) => new()
    {
        Type = type,
        Id = @event.Id,
        Ref = @event.Ref,
        Summary = @event.Summary,
        UserName = @event.UserName,
        IsVipUser = @event.AlertStatus is VipAlertStatus,
        AssignedDeptName = EventDepartments.GetName(@event.AssignedDepartment),
        AssignedAt = @event.AssignedAt.TruncateToSeconds()
    };

    [LoggerMessage(LogLevel.Debug, "Event {Ref} within SLA alert threshold: {Elapsed} elapsed ({Progress:P})")]
    partial void LogEventWithinSla(string @ref, TimeSpan elapsed, double progress);

    [LoggerMessage(LogLevel.Debug, "Event {Ref} nearing SLA breach: {Elapsed} elapsed ({Progress:P})")]
    partial void LogEventNearingBreachSla(string @ref, TimeSpan elapsed, double progress);

    [LoggerMessage(LogLevel.Debug, "Event {Ref} breached SLA: {Elapsed} elapsed ({Progress:P})")]
    partial void LogEventBreachedSla(string @ref, TimeSpan elapsed, double progress);
}
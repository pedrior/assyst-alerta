using Assyst.Alerta.Extensions;
using Assyst.Alerta.Models;

namespace Assyst.Alerta.Processing.Evaluators;

internal sealed class EventReopenEvaluator : IEventEvaluator
{
    public EventAlert? Evaluate(Event evt)
    {
        EventAlert? latestAlert = null;
        var hasSeenPendingClosure = false;

        // Evaluate from oldest to newest to verify the sequence
        var sortedActions = evt.Actions.OrderBy(a => a.CreatedAt);

        foreach (var action in sortedActions)
        {
            if (action.Type.Code is "PENDING-CLOSURE")
            {
                hasSeenPendingClosure = true;
            }
            else if (action.Type.Code is "RE-OPEN" && hasSeenPendingClosure)
            {
                // Capture the valid reopen. If there's another one later, it will overwrite this.
                latestAlert = new EventAlert
                {
                    Type = AlertType.Reopened,
                    Id = evt.Id,
                    ActionId = action.Id, // Essential for the CallbackFilter deduplication
                    Ref = evt.Ref,
                    Summary = evt.Summary,
                    UserName = evt.UserName,
                    IsVipUser = evt.AlertStatus is "RED",
                    AssignedDeptName = EventDepartments.GetName(evt.AssignedDepartment),
                    AssignedAt = evt.AssignedAt.TruncateToSeconds(),
                    ReopenedAt = action.CreatedAt
                };

                // Reset flag so the next RE-OPEN requires a new PENDING-CLOSURE
                hasSeenPendingClosure = false;
            }
            else if (action.Type.Code is "EM_ATENDIMENTO_1N" or "EM_ATENDIMENTO_2N" or "EM_ATENDIMENTO_3N")
            {
                // Cancel the alert if this occurs after a valid reopen
                latestAlert = null;
            }
        }

        return latestAlert;
    }
}
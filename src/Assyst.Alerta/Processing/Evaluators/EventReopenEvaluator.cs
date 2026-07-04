using Assyst.Alerta.Extensions;
using Assyst.Alerta.Models;

namespace Assyst.Alerta.Processing.Evaluators;

internal sealed class EventReopenEvaluator : IEventEvaluator
{
    public EventAlert? Evaluate(Event evt)
    {
        EventAlert? latestAlert = null;
        bool hasSeenPendingClosure = false;

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
                    Id = 0,
                    ActionId = action.Id, // Essential for the CallbackFilter deduplication
                    Ref = evt.Ref,
                    Summary = evt.Summary,
                    UserName = evt.UserName,
                    IsVipUser = evt.AlertStatus is "RED",
                    AssignedDeptName = EventDepartments.GetName(evt.AssignedDeptId),
                    AssignedAt = evt.AssignedAt.TruncateToSeconds(),
                    ReopenedAt = action.CreatedAt
                };

                // Reset flag so the next RE-OPEN requires a new PENDING-CLOSURE
                hasSeenPendingClosure = false; 
            }
        }

        return latestAlert;
    }
}
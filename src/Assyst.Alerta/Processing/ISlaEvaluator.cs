using Assyst.Alerta.Models;

namespace Assyst.Alerta.Processing;

internal interface ISlaEvaluator
{
    EventAlert? Evaluate(Event @event, DateTimeOffset now);
}

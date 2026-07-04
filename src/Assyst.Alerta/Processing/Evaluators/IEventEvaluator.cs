using Assyst.Alerta.Models;

namespace Assyst.Alerta.Processing.Evaluators;

internal interface IEventEvaluator
{
    public EventAlert? Evaluate(Event evt);
}
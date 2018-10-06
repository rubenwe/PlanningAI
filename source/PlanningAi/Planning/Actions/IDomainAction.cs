using System.Collections.Generic;

namespace PlanningAi.Planning.Actions
{
    public interface IDomainAction
    {
        string ActionName { get; }
        bool IsValid { get; }
        Dictionary<string, object> Preconditions { get; }
        Dictionary<string, object> Effects { get; }
        float GetCost(DomainState currentState);
    }
}
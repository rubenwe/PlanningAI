using JetBrains.Annotations;
using PlanningAi.Planning;

namespace PlanningAi.Agents
{
    [PublicAPI]
    public interface IConsideration
    {
        string Name { get; }
        float GetValue(DomainState currentState);
    }
}
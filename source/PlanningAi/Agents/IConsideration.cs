using JetBrains.Annotations;

namespace PlanningAi.Agents
{
    [PublicAPI]
    public interface IConsideration
    {
        string Name { get; }
        float GetValue();
    }
}
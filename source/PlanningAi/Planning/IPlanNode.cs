using JetBrains.Annotations;
using PlanningAi.Planning.Actions;

namespace PlanningAi.Planning
{
    [PublicAPI]
    public interface IPlanNode
    {
        IPlanNode Parent { get; }
        int Level { get; }
        float RunningCost { get; }
        float Estimate { get; }
        float TotalCost { get; }
        DomainState State { get; }
        IDomainAction SelectedAction { get; }
    }
}
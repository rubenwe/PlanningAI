using System;
using System.Diagnostics.CodeAnalysis;
using PlanningAi.Planning.Actions;

namespace PlanningAi.Planning
{
    public class PlanNode : IPlanNode
    {
        public IPlanNode Parent { get; }
        public int Level { get; }
        
        public float RunningCost { get; }
        public float Estimate { get; }
        public float TotalCost => RunningCost + Estimate;
        
        public DomainState State { get; }
        public IDomainAction SelectedAction { get; }
        
        public PlanNode(DomainState state, IDomainAction selectedAction, IPlanNode parent, float estimate)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            SelectedAction = selectedAction;
            Parent = parent;
            Estimate = estimate;
            RunningCost = (parent?.RunningCost ?? 0) + (selectedAction?.GetCost(parent?.State) ?? 0);
            Level = (parent?.Level ?? -1) + 1;
        }

        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            return $"{nameof(SelectedAction)}: {SelectedAction}, {nameof(RunningCost)}: {RunningCost}, {nameof(Level)}: {Level}";
        }
    }
}
using System.Collections.Generic;
using PlanningAi.Planning;

namespace PlanningAi.Agents
{
    public abstract class AgentGoalBase : IAgentGoal
    {
        private readonly List<IConsideration> _considerations = new List<IConsideration>();
        public IReadOnlyList<IConsideration> Considerations => _considerations;

        public void AddConsiderations(IEnumerable<IConsideration> considerations)
        {
            _considerations.AddRange(considerations);
        }
        
        public float Weight { get; set; }
        public abstract string GoalName { get; }

        public virtual void OnActivation(ref DomainState currentState, ref DomainState goalState)
        {
        }
    }
}
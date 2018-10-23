using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        public void AddConsideration(IConsideration consideration)
        {
            _considerations.Add(consideration);
        }
        
        public float Weight { get; set; }
        public virtual string GoalName => GetType().Name;

        [SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery")]
        public float GetGoalWeight(DomainState currentState)
        {
            if (Considerations.Count == 0) return Weight;
        
            var score = 1.0f;
            foreach (var consideration in Considerations)
            {
                score *= consideration.GetValue(currentState);
            }

            var modFactor = 1 - 1 / Considerations.Count;
            var makeUpFactor = (1 - score) * modFactor;
            var considerationScore = score + makeUpFactor * score;

            return considerationScore * Weight;
        }

        public virtual void OnActivation(ref DomainState currentState, ref DomainState goalState)
        {
        }
    }
}
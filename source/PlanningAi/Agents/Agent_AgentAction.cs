using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PlanningAi.Planning;
using PlanningAi.Planning.Actions;

namespace PlanningAi.Agents
{
    public partial class Agent
    {
        [PublicAPI]
        protected class AgentAction : IAsyncExecutableAction
        {
            private readonly Agent _agent;
            private readonly IAsyncExecutableAction _action;

            private readonly List<IAgentGoal> _validGoals = new List<IAgentGoal>();
            
            public string ActionName => _action.ActionName;
            public bool IsValid => _action.IsValid && (!_validGoals.Any() || _validGoals.Contains(_agent.CurrentGoal));
            
            public AgentAction(Agent agent, IAsyncExecutableAction action)
            {
                _action = action;
                _agent = agent;
            }

            public Dictionary<string, object> Preconditions => _action.Preconditions;
            public Dictionary<string, object> Effects => _action.Effects;
            
            public float GetCost(DomainState currentState) => _action.GetCost(currentState);
            public Task<bool> ExecuteAsync(DomainState state, CancellationToken token) => _action.ExecuteAsync(state);
            
            public AgentAction BindTo(IAgentGoal goal)
            {
                _validGoals.Add(goal);

                return this;
            }

            public override string ToString() => _action.ToString();
        }
    }
}
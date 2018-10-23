using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PlanningAi.Planning;
using PlanningAi.Planning.Actions;

namespace PlanningAi.Agents
{
    [PublicAPI]
    public interface IAgent
    {
        IReadOnlyList<IAgentGoal> Goals { get; } 
        IReadOnlyList<IDomainAction> Actions { get; }
        
        DomainState CurrentState { get; }
        IAgentGoal CurrentGoal { get; }
        IDomainAction CurrentAction { get; }

        void AddAction(IDomainAction action);
        void AddGoal(IAgentGoal goal);
        
        Task RunActionsAsync(CancellationToken token = default);
    }
}
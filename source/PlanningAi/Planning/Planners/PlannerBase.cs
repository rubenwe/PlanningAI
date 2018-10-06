using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using PlanningAi.Planning.Actions;
using PlanningAi.Planning.Utils;

namespace PlanningAi.Planning.Planners
{
    using Actions = IReadOnlyList<IDomainAction>;
    using IDebugGraph = IReadOnlyGraph<IPlanNode, IDomainAction>;
    
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Utility Class")]
    public abstract class PlannerBase : IPlanner
    {
        private readonly bool _debuggingEnabled;
        private readonly bool _earlyExit;

        protected PlannerBase(bool earlyExit = true, bool debuggingEnabled = false)
        {
            _debuggingEnabled = debuggingEnabled;
            _earlyExit = earlyExit;
        }
        
        public abstract PlanningResult GetPlan(DomainState startState, DomainState goalState, ActionSet actions);
        
        protected (PlanNode, IDebugGraph) FindPlan(
            PlanNode start, 
            Func<DomainState, bool> isTarget,
            Func<PlanNode, IEnumerable<PlanNode>> getOptions)
        {
            var visitedStates = new HashSet<DomainState>();
            var planQueue = new PriorityQueue<PlanNode>();

            var debugGraph = _debuggingEnabled
                ? new DebugGraph() 
                : null;
            
            planQueue.Enqueue(0, start);
            debugGraph?.AddNode(start);

            while (!planQueue.IsEmpty)
            {
                var currentNode = planQueue.Dequeue();
                var currentState = currentNode.State;

                if (visitedStates.Contains(currentState)) continue;

                if (isTarget(currentState))
                {
                    return (currentNode, debugGraph);
                }

                visitedStates.Add(currentState);

                foreach (var planNode in getOptions(currentNode))
                {
                    planQueue.Enqueue(planNode.TotalCost, planNode);

                    debugGraph?.AddNode(planNode);
                    debugGraph?.AddEdge(planNode.Parent, planNode, planNode.SelectedAction);

                    if (_earlyExit && isTarget(planNode.State))
                    {
                        return (planNode, debugGraph);
                    }
                }
            }
            
            return (null, debugGraph);
        }

        protected static Actions GetActionsInverted(IPlanNode node)
        {
            var stack = new Stack<IPlanNode>();
            var current = node;
            while (current != null)
            {
                stack.Push(current);
                current = current.Parent;
            }

            return stack
                .Select(n => n.SelectedAction)
                .Where(action => action != null)
                .ToList();
        }

        protected static Actions GetActions(IPlanNode node)
        {
            var queue = new Queue<IPlanNode>();
            var current = node;
            while (current != null)
            {
                queue.Enqueue(current);
                current = current.Parent;
            }

            return queue
                .Select(n => n.SelectedAction)
                .Where(action => action != null)
                .ToList();
        }
        
        private class DebugGraph : SimpleGraph<IPlanNode, IDomainAction>
        {
        }
    }
}
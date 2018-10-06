using System.Collections.Generic;
using PlanningAi.Planning.Actions;

namespace PlanningAi.Planning.Planners
{
    using Preconditions = Dictionary<string, object>;
    using Plan = IEnumerable<PlanNode>;

    internal class RegressivePlanner : PlannerBase
    {
        public RegressivePlanner(bool earlyExit = true, bool enableDebugging = false) 
            : base(earlyExit, enableDebugging) { }
        
        public override PlanningResult GetPlan(DomainState startState, DomainState goalState, ActionSet actions)
        {
            Plan GetOptions(IPlanNode node) => RegressivePlanner.GetOptions(node, actions, startState);
            
            var start = new PlanNode(goalState, null, null, 0);
            var (path, graph) = FindPlan(start, startState.IsSuperstateOf, GetOptions);

            return new PlanningResult
            {
                Plan = GetActions(path),
                SearchTree = graph
            };
        }

        private static Plan GetOptions(IPlanNode node, ActionSet actions, DomainState startState)
        {
            var goalState = node.State;
            var expandedActions = new HashSet<IDomainAction>();
            
            foreach (var (worldVar, targetValue) in goalState.GetAll())
            {
                if (!actions.TryGetOut(worldVar, out var matchingActions)) continue;

                // TODO create path for numeric values
                foreach (var (action, effectValue) in matchingActions)
                {
                    if (expandedActions.Contains(action)) continue;
                    if (!Equals(effectValue, targetValue)) continue;

                    // Discard actions with effects that don't overlap with goal state
                    if (!TryApplyEffects(action, goalState, out var nextStateToFind)) continue;
                    
                    // Check if precondition violates search
                    if (AnyPreconditionConflictsWithGoal(action.Preconditions, nextStateToFind)) continue;
                    
                    // And inject new preconditions into new search state
                    nextStateToFind = nextStateToFind.SetRange(action.Preconditions);

                    // No need to expand action again
                    expandedActions.Add(action);
                    
                    yield return new PlanNode(nextStateToFind, action, node, nextStateToFind.DistanceTo(startState));
                }
            }
        }
        
        private static bool AnyPreconditionConflictsWithGoal(Preconditions preconditions, DomainState goalState)
        {
            foreach (var worldVar in preconditions.Keys)
            {
                if(!goalState.TryGet(worldVar, out object searchedValue)) continue;
                
                var newValue = preconditions[worldVar];
                
                if (!Equals(newValue, searchedValue)) return true;
            }

            return false;
        }

        private static bool TryApplyEffects(IDomainAction action, DomainState goalState, out DomainState newState)
        {
            newState = goalState;
            
            foreach (var pair in action.Effects)
            {
                var worldVar = pair.Key;

                // Preconditions aren't violated, so we just ignore this
                if (!goalState.TryGet(worldVar, out object value)) continue;
                
                // If any effect clashes with the goal state, the plan node is invalid
                if (!Equals(value, pair.Value)) return false;
                    
                // If the goal is fulfilled, we can remove it from the search
                newState = newState.Remove(worldVar);
            }

            return true;
        }
    }
}
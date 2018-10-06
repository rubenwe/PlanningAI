using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PlanningAi.Planning;
using PlanningAi.Planning.Actions;
using PlanningAi.Planning.Planners;
using PlanningAi.Utils.Logging;

namespace PlanningAi.Agents
{
    [PublicAPI]
    [SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
    public partial class Agent : IAgent
    {
        private readonly Queue<IDomainAction> _actionQueue = new Queue<IDomainAction>();
        private readonly List<IDomainAction> _actions = new List<IDomainAction>();
        private readonly List<IAgentGoal> _goals = new List<IAgentGoal>();
        private readonly IPlanner _planner;
        private readonly ILogger _logger;
        
        public DomainState CurrentState { get; private set; } = DomainState.Empty;
        public IAgentGoal CurrentGoal { get; private set; }
        public IDomainAction CurrentAction { get; private set; }

        public IReadOnlyList<IDomainAction> Actions => _actions;
        public IReadOnlyList<IAgentGoal> Goals => _goals;

        public Agent(IPlanner planner, ILogger logger = null)
        {
            _planner = planner ?? throw new ArgumentNullException(nameof(planner));
            _logger = logger ?? new DummyLogger();
        }
        
        /// <summary>
        /// Adds an action to the agent.
        /// </summary>
        /// <param name="action">The action to add.</param>
        [PublicAPI]
        public void AddAction(IDomainAction action)
        {
            _actions.Add(action);
        }
        
        /// <summary>
        /// Adds a goal to an agent.
        /// </summary>
        /// <param name="goal">The goal to add</param>
        /// <exception cref="ArgumentException">If goal has no considerations.</exception>
        [PublicAPI]
        public void AddGoal(IAgentGoal goal)
        {
            _goals.Add(goal);
        }

        public async void RunActions(CancellationToken token = default)
        {
            while (!token.IsCancellationRequested)
            {
                CurrentAction = null;
                
                var state = GetWorldState();

                if (_actionQueue.Count == 0)
                {
                    _logger.Trace("ActionQueue of agent is empty. Replanning.");
                    state = await ReplanAsync(state, token);
                    
                    CurrentState = state;
                }

                var action = _actionQueue.Dequeue();
                if (action.HasUnsatisfiedPreconditions(state))
                {
                    OnActionHasUnsatisfiedPreconditions(action, state);
                    continue;
                }

                if (action is IAsyncExecutableAction executableAction)
                {
                    await ExecuteAction(executableAction, state, token);
                }
            }

            _logger.Trace("Quitting, because cancellation was requested");
        }

        protected virtual DomainState GetWorldState()
        {
            return CurrentState;
        }
        
        protected virtual void OnActionHasUnsatisfiedPreconditions(IDomainAction action, DomainState state)
        {
            var unsatisfiedConditions = action.Preconditions
                .Where(cond => !state.Fulfills(cond.Key, cond.Value))
                .Select(cond => $"({cond.Key}, {cond.Value} != {state.GetValueOrDefault<object>(cond.Key)})");

            var conditionString = string.Join(", ", unsatisfiedConditions);
            
            _logger.Warn("Action `{0}` has unsatisfied preconditions [{1}]. Flushing ActionQueue.", action.ActionName, conditionString);
            
            _actionQueue.Clear();
        }

        protected virtual IAgentGoal OnNoGoalFound(ref DomainState state)
        {
            _logger.Error("Could not find a new goal for planning!");
            return null;
        }
        
        protected virtual IAgentGoal OnNewGoalSelected(IAgentGoal goal)
        {
            _logger.Debug("New goal selected: `{0}`", goal.GoalName);
            return goal;
        }
        
        protected virtual void OnNewPlanCreated(IReadOnlyList<IDomainAction> plan)
        {
            _logger.Debug("New plan created: {0}", string.Join(", ", plan.Select(a => a.ToString())));
        }

        protected virtual void OnActionStarting(IDomainAction action)
        {
            _logger.Trace("Executing action `{0}`", action.ActionName);
        }

        protected virtual void OnActionCompleted(IDomainAction action)
        {
            _logger.Trace("Execution of action `{0}` was successful.", action.ActionName);
            CurrentState = CurrentState.Apply(action.Effects);
        }

        protected virtual void OnActionFailed(IDomainAction action)
        {
            _logger.Debug("Execution of action `{0}` failed. Flushing ActionQueue.", action.ActionName);
            _actionQueue.Clear();
        }
        
        private async Task<DomainState> ReplanAsync(DomainState state, CancellationToken token)
        {
            var goalState = SetNextGoal(ref state);
            if (goalState == null)
            {
                throw new InvalidOperationException("Agent could not find goal to fulfill!");
            }

            await CreateNewPlanAsync(state, goalState, token);
            
            if (_actionQueue.Count == 0)
            {
                throw new InvalidOperationException("ActionQueue of agent could not be filled!");
            }

            OnNewPlanCreated(_actionQueue.ToList());

            return state;
        }

        private DomainState SetNextGoal(ref DomainState state)
        {
            var goal = FindBestRatedGoal();

            if (goal == null)
            {
                var fallBack = OnNoGoalFound(ref state);
                if (fallBack == null) return null;

                goal = fallBack;
            }
            
            var goalState = DomainState.Empty;

            CurrentGoal = OnNewGoalSelected(goal);
            
            goal.OnActivation(ref state, ref goalState);
            
            return goalState;
        }

        private async Task CreateNewPlanAsync(DomainState state, DomainState goalState, CancellationToken token)
        {
            var actionSet = GetActionSet();
            
            try
            {
                _logger.Debug("Starting planning to reach goal `{0}`.", CurrentGoal.GoalName);
                
                var result = await Task.Run(() => _planner.GetPlan(state, goalState, actionSet), token);
                if (!result.Success) return;
                
                foreach (var action in result.Plan)
                {
                    _actionQueue.Enqueue(action);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Debug("Planning was canceled.");
            }
        }
        
        private ActionSet GetActionSet()
        {
            return new ActionSet(Actions.Where(action => action.IsValid));
        }

        private async Task ExecuteAction(IAsyncExecutableAction action, DomainState currentState, CancellationToken token)
        {
            OnActionStarting(action);
            CurrentAction = action;

            try
            {
                var actionSucceeded = await action.ExecuteAsync(currentState, token);
                if (actionSucceeded)
                {
                    OnActionCompleted(action);
                }
                else
                {
                    OnActionFailed(action);
                }
            }
            catch (OperationCanceledException)
            {
                CurrentAction = null;
            }
        }

        private IAgentGoal FindBestRatedGoal()
        {
            _logger.Debug("Searching goal for next plan, was `{0}`.", CurrentGoal?.GoalName ?? "None");
            
            var currentMax = float.MinValue;
            IAgentGoal bestGoal = null;
            
            foreach (var goal in Goals)
            {
                var goalScore = CalculateGoalScore(goal);
                if (!(goalScore > currentMax)) continue;
                
                currentMax = goalScore;
                bestGoal = goal;
            }

            return bestGoal;
        }

        // ReSharper disable once LoopCanBeConvertedToQuery
        private static float CalculateGoalScore(IAgentGoal goal)
        {
            var score = 1.0f;
            foreach (var consideration in goal.Considerations)
            {
                score *= consideration.GetValue();
            }

            var modFactor = 1 - 1 / goal.Considerations.Count;
            var makeUpFactor = (1 - score) * modFactor;
            var considerationScore = score + makeUpFactor * score;
            
            return considerationScore * goal.Weight;
        }
        
        /// <summary>
        /// Adds an action to the agent.
        /// </summary>
        /// <param name="action">The action to add.</param>
        /// <returns>Wrapped action that can be used to bind the action to goals.</returns>
        [PublicAPI]
        protected AgentAction AddBindableAction(IAsyncExecutableAction action)
        {
            var agentAction = new AgentAction(this, action);
            _actions.Add(agentAction);
            
            return agentAction;
        }

        /// <summary>
        /// Adds a goal to an agent.
        /// </summary>
        /// <param name="weight">The weight of the goal.</param>
        /// <param name="considerations">The considerations for the goal.</param>
        /// <typeparam name="TGoal">The type of goal that should be added.</typeparam>
        /// <returns>Instance of that goal - with weight and considerations set.</returns>
        /// <exception cref="ArgumentException">If goal has no considerations.</exception>
        [PublicAPI]
        protected IAgentGoal AddGoal<TGoal>(float weight, params IConsideration[] considerations) where TGoal : AgentGoalBase, new()
        {
            if(considerations.Length == 0) throw new ArgumentException("Goal must have at least one consideration.");
            
            var goal = new TGoal { Weight = weight };
            
            goal.AddConsiderations(considerations);
            
            _goals.Add(goal);

            return goal;
        }
    }
}
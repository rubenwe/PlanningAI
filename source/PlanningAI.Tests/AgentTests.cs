using System;
using System.Threading;
using System.Threading.Tasks;
using PlanningAi.Agents;
using PlanningAi.Planning;
using PlanningAi.Planning.Actions;
using PlanningAi.Planning.Planners;
using Xunit;

namespace PlanningAI.Tests
{
    public class AgentTests
    {
        [Fact]
        public async Task BindableActionsShouldBeAvailable()
        {
            var planner = PlannerFactory.CreatePlanner();
            var agent = new TestAgent(planner);

            await Assert.ThrowsAsync<NoRunException>(
                async () => await agent.RunActionsAsync());
        }
        
        [Fact]
        public async Task CancellationOfRunningActionsShouldWork()
        {
            var goal = new TestGoal{Weight = 1};
            var action = new InfiniteRunAction();
            
            var planner = PlannerFactory.CreatePlanner();
            var agent = new Agent(planner);
            
            var source = new CancellationTokenSource();
            
            agent.AddGoal(goal);
            agent.AddAction(action);

            var agentTask = agent.RunActionsAsync(source.Token);
            var cancelTask = Task.Run(() => source.Cancel());

            await Task.WhenAll(agentTask, cancelTask);

            Assert.Equal(action, agent.CurrentAction);
        }
        
        [Fact]
        public async Task ShouldRunActionToReachGoal()
        {
            var goal = new TestGoal{Weight = 1};
            var planner = PlannerFactory.CreatePlanner();
            var agent = new FallbackAgent(planner, goal);
            
            agent.AddGoal(goal);
            agent.AddAction(new NoRunAction());

            await Assert.ThrowsAsync<NoRunException>(
                async () => await agent.RunActionsAsync());
        }
        
        [Fact]
        public async Task ShouldUseFallbackGoal()
        {
            var goal = new TestGoal{Weight = 1};
            var planner = PlannerFactory.CreatePlanner();
            var agent = new FallbackAgent(planner, goal);

            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await agent.RunActionsAsync());
            
            Assert.Equal(goal, agent.CurrentGoal);
        }

        [Fact]
        public async Task ShouldThrowExceptionIfNoGoalsSupplied()
        {
            var planner = PlannerFactory.CreatePlanner();
            var agent = new Agent(planner);

            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await agent.RunActionsAsync());
        }
        
        [Fact]
        public async Task ShouldChooseHighestRatedGoal()
        {
            var planner = PlannerFactory.CreatePlanner();
            var agent = new Agent(planner);

            var goal1 = new TestGoal{Weight = 0.2f};
            var goal2 = new TestGoal{Weight = 0.5f};
            
            agent.AddGoal(goal1);
            agent.AddGoal(goal2);

            await Assert.ThrowsAsync<InvalidOperationException>(
                async() => await agent.RunActionsAsync());
            
            Assert.Equal(goal2, agent.CurrentGoal);
        }

        [Fact]
        public async Task ShouldUseConsiderationToChooseBestGoal()
        {
            var planner = PlannerFactory.CreatePlanner();
            var agent = new Agent(planner);

            var goal1 = new TestGoal{Weight = 1};
            var goal2 = new TestGoal{Weight = 1};
            
            goal1.AddConsideration(Consideration.FromFunc(d => 0.5f));
            goal2.AddConsideration(Consideration.FromFunc(d => 0.2f));
            
            agent.AddGoal(goal1);
            agent.AddGoal(goal2);

            await Assert.ThrowsAsync<InvalidOperationException>(
                async() => await agent.RunActionsAsync());
            
            Assert.Equal(goal1, agent.CurrentGoal);
        }
    }

    public abstract class GoalAction : AsyncExecutableActionBase
    {
        protected GoalAction()
        {
            Preconditions.Add("isGoal", false);
            Effects.Add("isGoal", true);
        }
    }

    public class InfiniteRunAction : GoalAction
    {
        public override async Task<bool> ExecuteAsync(DomainState currentState, CancellationToken token)
        {
            var source = new TaskCompletionSource<bool>();
            token.Register(() => source.SetResult(true));
            await source.Task;
            
            return true;
        }
    }

    public class NoRunAction : GoalAction
    {
        public override Task<bool> ExecuteAsync(DomainState currentState, CancellationToken token)
        {
            throw new NoRunException();
        }
    }

    public class NoRunException : Exception
    {
    }

    public class TestAgent : Agent
    {
        public float ConsValue { get; set; } = 1f;
        public TestAgent(IPlanner planner) : base(planner)
        {
            var goal = AddGoal<TestGoal>(1f, Consideration.FromFunc(() => ConsValue));
            var action = AddBindableAction(new NoRunAction());
            action.BindTo(goal);
        }
    }
    
    public class FallbackAgent : Agent
    {
        private readonly IAgentGoal _fallback;

        public FallbackAgent(IPlanner planner, IAgentGoal fallback) : base(planner)
        {
            _fallback = fallback;
        }

        protected override IAgentGoal OnNoGoalFound(ref DomainState state)
        {
            base.OnNoGoalFound(ref state);
            
            return _fallback;
        }
    }
    
    public class TestGoal : AgentGoalBase
    {
        public override void OnActivation(ref DomainState currentState, ref DomainState goalState)
        {
            goalState = goalState.Set("isGoal", true);
        }
    }
}
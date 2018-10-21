using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json.Serialization;
using PlanningAi.Planning;
using PlanningAi.Planning.Actions;
using PlanningAi.Planning.Planners;
using PlanningAi.Utils;
using Xunit;

namespace PlanningAI.Tests
{
    public class ResourcePoolTests
    {
        [Fact]
        public async Task ShouldSearchAllWaitingRequests()
        {
            var executedChecks = 0;
            Func<int, bool> nope = i =>
            {
                executedChecks++;
                return false;
            };
            
            var pool = new ResourcePool<int>(1, 2, 3);

            var item1 = await pool.RentAsync();
            var task2 = pool.RentAsync(nope);
            var task3 = pool.RentAsync(nope);

            
            var initialChecks = (pool.ItemCount - 1) * 2;
            
            Assert.Equal(initialChecks, executedChecks);
           
            executedChecks = 0;
            pool.Return(item1);
            
            var checksOnReturn = pool.ItemCount * 2;
            Assert.Equal(checksOnReturn, executedChecks);
        }
        
        [Fact]
        public async Task OwnerViewShouldAllowToReturnAllOwned()
        {
            var owner1 = new object();
            var pool = new ResourcePool<string>("1", "a", "2");
            using (var view = pool.GetViewForOwner(owner1))
            {
                var item1 = await view.RentAsync(s => int.TryParse(s, out _));
                var item2 = await view.RentAsync(s => int.TryParse(s, out _));
                var item3 = await pool.RentAsync();

                Assert.Equal("1", item1);
                Assert.Equal("2", item2);
                Assert.Equal("a", item3);

                await Assert.ThrowsAsync<TaskCanceledException>(async () =>
                {
                    var source = new CancellationTokenSource(1);
                    await pool.RentAsync(source.Token);
                });
            }

            var item4 = await pool.RentAsync();
            var item5 = await pool.RentAsync();
            
            Assert.Equal("1", item4);
            Assert.Equal("2", item5);
        }
        
        [Fact]
        public void CheckingIfUnknownResourceIsRentedShouldThrow()
        {
            var pool = new ResourcePool<object>();
            Assert.Throws<ArgumentException>(() => pool.IsRented(new object()));
        }
        
        [Fact]
        public async Task OwnedResourceShouldBeReturnableForOwner()
        {
            var owner1 = new object();
            
            var pool = new ResourcePool<string>("a", "b");
            var resource1 = await pool.RentAsync(owner1);

            pool.Return(owner1, resource1);

            Assert.False(pool.IsRented(resource1));
        }
        
        [Fact]
        public async Task ShouldNotAllowToReturnUnownedResource()
        {
            var owner1 = new object();
            var owner2 = new object();
            
            var pool = new ResourcePool<string>("a", "b");
            var resource1 = await pool.RentAsync(owner1);

            Assert.Throws<InvalidOperationException>(() => { pool.Return(owner2, resource1); });
        }

        [Fact]
        public async Task ShouldNotAllowToReturnFreeResource()
        {
            var pool = new ResourcePool<string>("a", "b");
            var resource1 = await pool.RentAsync();
            
            pool.Return(resource1);
            Assert.Throws<InvalidOperationException>(() => pool.Return(resource1));
        }
        
        [Fact]
        public async Task CancelShouldFreeTasks()
        {
            var pool = CreateResourcePool();
            var source = new CancellationTokenSource();

            // Get resource to block pool
            _ = await pool.RentAsync(source.Token);
            var task1 = pool.RentAsync(source.Token);
            
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                source.Cancel();
                await task1;
            });

            Assert.True(task1.IsCanceled);
        }
        
        [Fact]
        public async Task ReleasingShouldAllowWaitingTasksToContinue()
        {
            var pool = CreateResourcePool();
            var secondRented = false;
            var canRentCalls = 0;

            var cook1Task = pool.RentAsync(c =>
            {
                canRentCalls++;
                return secondRented;
            });
            
            var cook = await pool.RentAsync();
            secondRented = true;
            pool.Return(cook);

            cook = await cook1Task;
            
            Assert.NotNull(cook);
            Assert.Equal(2, canRentCalls);
        }

        [Fact]
        public async Task ShouldPrioritizeResourceWithLowestPriorityValue()
        {
            var cookA = new Cook();
            var cookB = new Cook();
            var pool = new ResourcePool<Cook>(cookA, cookB);

            var cheaperCook = await pool.RentAsync(null, c => c == cookA ? 5 : 3);
            
            Assert.Equal(cookB, cheaperCook);
        }

        [Fact]
        public async Task CookScenario()
        {
            var pool = CreateResourcePool();
            var provider = new WorldStateProvider();
            var planner = PlannerFactory.CreatePlanner();
            var actionSet = new ActionSet(GetDomainActions(pool, provider));

            var ingredients = new[] {Ingredient.CookedPatty, Ingredient.Bread, Ingredient.Cheese};

            var tasks = new List<Task>();
            var executedActions = new List<string>();
            foreach (var ingredient in ingredients)
            {
                var goalState = DomainState.Empty.Set(ingredient + "Delivered", true);
                var result = planner.GetPlan(provider.GetCurrentState(null), goalState, actionSet);
                Assert.True(result.Success);

                tasks.Add(RunPlan(result.Plan, executedActions));
            }

            await Task.WhenAll(tasks);

            Assert.NotEmpty(executedActions);
            Assert.Equal("Starting: Pick Up RawPatty", executedActions.First());
        }

        [Fact]
        public async Task AddingResourceToPoolShouldMakeItAvailable()
        {
            var pool = new ResourcePool<string>();
            Assert.Equal(0, pool.ItemCount);

            var expected = "test";
            
            pool.Add(expected);
            Assert.Equal(1, pool.ItemCount);
            var rented = await pool.RentAsync();

            Assert.Equal(expected, rented);
        }
        
        [Fact]
        public async Task AddingMultipleResourcesToPoolShouldMakeThemAvailable()
        {
            var pool = new ResourcePool<string>();
            Assert.Equal(0, pool.ItemCount);

            var test1 = "test1";
            var test2 = "test2";
            var test3 = "test3";
            
            pool.Add(test1);
            Assert.Equal(1, pool.ItemCount);
            
            var rented1 = await pool.RentAsync();
            Assert.Equal(test1, rented1);            

            pool.AddRange(new[]{test2, test3});
            var rented2 = await pool.RentAsync();
            Assert.Equal(test2, rented2);
            
            var rented3 = await pool.RentAsync();
            Assert.Equal(test3, rented3);
        }

        private static ResourcePool<Cook> CreateResourcePool()
        {
            var cooks = new[] {new Cook()};
            var pool = new ResourcePool<Cook>(cooks);
            return pool;
        }

        private async Task RunPlan(IReadOnlyList<IDomainAction> plan, List<string> executedActions)
        {
            foreach (var action in plan.OfType<CookActionBase>())
            {
                //executedActions.Add("Waiting to start: " + action.ActionName);
                action.Started += () => executedActions.Add("Starting: " + action.ActionName);
                
                var result = await action.ExecuteAsync();
                Assert.True(result);
                
                executedActions.Add("Finished: " + action.ActionName);
            }
        }

        private static List<CookActionBase> GetDomainActions(ResourcePool<Cook> pool, WorldStateProvider provider)
        {
            var actions = new List<CookActionBase>
            {
                new PickUpIngredientAction(Ingredient.Bread),
                new PickUpIngredientAction(Ingredient.Tomato),
                new PickUpIngredientAction(Ingredient.Cheese),
                new PickUpIngredientAction(Ingredient.RawPatty),
                new CookPattyAction(),
                new DeliverIngredientAction(Ingredient.Bread),
                new DeliverIngredientAction(Ingredient.Tomato),
                new DeliverIngredientAction(Ingredient.Cheese),
                new DeliverIngredientAction(Ingredient.CookedPatty),
            };
            
            actions.ForEach(a => a.Initialize(pool, provider));
            
            return actions;
        }
 
    }

    internal class WorldStateProvider : IWorldStateProvider
    {
        public DomainState GetCurrentState(Cook cook)
        {
            return DomainState.Empty.Set("isCarrying", cook?.CarriedIngredient ?? default);
        }
    }

    internal class PickUpIngredientAction : CookActionBase
    {
        public override string ActionName => "Pick Up " + _ingredient;
        private readonly Ingredient _ingredient;

        public PickUpIngredientAction(Ingredient ingredient) 
        {
            _ingredient = ingredient;
            Preconditions.Add("isCarrying", Ingredient.None);
            Effects.Add("isCarrying", _ingredient);
        }
        
        protected override async Task<bool> ExecuteWithRentedCookAsync(CancellationToken token)
        {
            // Go to box and pick up ingredient
            await Task.Delay(1, token);

            RentedCook.CarriedIngredient = _ingredient;
            
            return true;
        }
    }

    internal class CookPattyAction : CookActionBase
    {
        public override string ActionName => "Cook patty";

        public CookPattyAction()
        {
            Preconditions.Add("isCarrying", Ingredient.RawPatty);
            Effects.Add("isCarrying", Ingredient.CookedPatty);
        }
        
        protected override async Task<bool> ExecuteWithRentedCookAsync(CancellationToken token)
        {
            // Go to pan and put patty in.
            await Task.Delay(1, token);
            RentedCook.CarriedIngredient = Ingredient.None;
            
            // Return cook for other tasks
            CookPool.Return(RentedCook);

            // Wait for patty to finish cooking
            await Task.Delay(1, token);

            RentedCook = await CookPool.RentAsync(c => c.CarriedIngredient == Ingredient.None, token);
            if (token.IsCancellationRequested) return false;

            // Go to pan and pick up patty
            await Task.Delay(1, token);
            RentedCook.CarriedIngredient = Ingredient.CookedPatty;
            
            return true;
        }
    }

    internal class DeliverIngredientAction : CookActionBase
    {
        private readonly Ingredient _ingredient;

        public DeliverIngredientAction(Ingredient ingredient)
        {
            _ingredient = ingredient;
            Preconditions.Add("isCarrying", _ingredient);
            Effects.Add(_ingredient + "Delivered", true);
            Effects.Add("isCarrying", Ingredient.None);
        }

        public override string ActionName => "Deliver " + _ingredient;
        protected override async Task<bool> ExecuteWithRentedCookAsync(CancellationToken token)
        {
            // go to burger
            await Task.Delay(1, token);

            RentedCook.CarriedIngredient = Ingredient.None;

            return true;
        }
    }
    
    internal abstract class CookActionBase : DomainActionBase
    {
        public event Action Started;
        
        private ResourcePool<Cook> _pool;
        private IWorldStateProvider _stateProvider;

        protected Cook RentedCook { get; set; }
        protected ResourcePool<Cook> CookPool => _pool;

        public void Initialize([NotNull] ResourcePool<Cook> cookPool, [NotNull] IWorldStateProvider stateProvider)
        {
            _pool = cookPool ?? throw new ArgumentNullException(nameof(cookPool));
            _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
        }

        public async Task<bool> ExecuteAsync(CancellationToken token = default)
        {
            RentedCook = await _pool.RentAsync(CanExecute, null, token);
            if (token.IsCancellationRequested) return false;
            
            Started?.Invoke();
            
            var result = await ExecuteWithRentedCookAsync(token);

            if (RentedCook != null)
            {
                _pool.Return(RentedCook);
            }
            
            return result;
        }
        
        private bool CanExecute(Cook cook)
        {
            var currentState = _stateProvider.GetCurrentState(cook);
            return !this.HasUnsatisfiedPreconditions(currentState);
        }

        protected abstract Task<bool> ExecuteWithRentedCookAsync(CancellationToken token);
    }
    
    

    internal interface IWorldStateProvider
    {
        DomainState GetCurrentState(Cook cook);
    }

    internal class Cook
    {
        public Ingredient CarriedIngredient { get; set; }
    }

    internal enum Ingredient
    {
        None,
        Tomato,
        RawPatty,
        CookedPatty,
        Cheese,
        Bread
    }
}
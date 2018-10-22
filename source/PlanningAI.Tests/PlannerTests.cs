using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PlanningAi.Planning;
using PlanningAi.Planning.Actions;
using PlanningAi.Planning.Planners;
using Xunit;

namespace PlanningAI.Tests
{
    public class PlannerTests
    {
        private const int BenchmarkIterations = 1;

        [Fact]
        public void FailedPlanningShouldReturnNullPlan()
        {
            var start = DomainState.Empty;
            var target = DomainState.Empty.Set("test", 123);
            var actions = new ActionSet(Enumerable.Empty<IDomainAction>());

            var planner = PlannerFactory.CreatePlanner();
            var result = planner.GetPlan(start, target, actions);

            Assert.False(result.Success);
            Assert.Null(result.Plan);
            Assert.Null(result.SearchTree);
        }
        
        [Fact]
        public void PlanningShouldFindWayToReachTargetValue()
        {
            var actionList = new List<IDomainAction>
            {
                new BuyAction("Axe"),
                new SellAction("Wood"),
                new EatPieAction(),
                new BuyAction("Pie"),
                new ChopWoodAction(),
                new TakeANapAction(),
                new GoToAction("Shop"),
                new GoToAction("Tree"),
                new GoToAction("Home"),
                new BuyAction("Alcohol"),
                new DrinkAlcoholAction()
            };

            var actions = new ActionSet(actionList);

            var start = DomainState.Empty
                .Set("isHungry", true)
                .Set("isSober", true)
                .Set("hasAxe", true);

            var goal = DomainState.Empty
                .Set("isHungry", false)
                .Set("isDrunk", true);

            var planner = PlannerFactory.CreatePlanner(
                new PlannerSettings
                {    
                    EarlyExit = false,
                    CreateDebugGraph = true,
                    PlannerType = PlannerType.Regressive
                });

            // WarmUp
//            for (var i = 0; i < 10; i++)
//            {
//                var _ = planner.GetPlan(start, goal, actions);
//            }

            PlanningResult result = null;
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < BenchmarkIterations; i++)
            {
                result = planner.GetPlan(start, goal, actions);
            }

            Assert.NotNull(result);
            
            Assert.NotNull(result.Plan);
            Assert.NotEmpty(result.Plan);

            Assert.NotNull(result.SearchTree);
            
            sw.Stop();
            
            //WritePlanToFile(plan, sw);
        }

//        private static void WritePlanToFile(IEnumerable<IDomainAction> plan, Stopwatch sw)
//        {
//            File.WriteAllLines("plan.txt", plan
//                .Select(p => p.ToString())
//                .Concat(new[]
//                {
//                    "",
//                    $"Time for plan: {sw.Elapsed.TotalMilliseconds / BenchmarkIterations:F2}ms"
//                }));
//
//            Process.Start("notepad.exe", "plan.txt");
//        }
    }

    
    
    public class TakeANapAction : DomainActionBase
    {
        public override string ActionName => "Take a nap";
        protected override float StaticCost => 3;

        public TakeANapAction()
        {
            Preconditions.Add("isNear", "Home");
            Preconditions.Add("isTired", true);

            Effects.Add("isTired", false);
        }
    }

    public class DrinkAlcoholAction : DomainActionBase
    {
        public override string ActionName => "Drink Alcohol";

        public DrinkAlcoholAction()
        {
            Preconditions.Add("hasAlcohol", true);
            Preconditions.Add("isSober", true);
            Preconditions.Add("isDrunk", false);

            Effects.Add("hasAlcohol", false);
            Effects.Add("isSober", false);
            Effects.Add("isDrunk", true);
        }
    }

    public class EatPieAction : DomainActionBase
    {
        public override string ActionName => "Eat Pie";

        public EatPieAction()
        {
            Preconditions.Add("hasPie", true);
            Preconditions.Add("isHungry", true);

            Effects.Add("isHungry", false);
            Effects.Add("hasPie", false);
        }
    }

    public class ChopWoodAction : DomainActionBase
    {
        protected override float StaticCost => 3;
        public override string ActionName => "Chop Wood";

        public ChopWoodAction()
        {
            Preconditions.Add("isTired", false);
            Preconditions.Add("hasAxe", true);
            Preconditions.Add("isNear", "Tree");

            Effects.Add("hasWood", true);
            Effects.Add("isTired", true);
        }
    }

    public class GoToAction : DomainActionBase
    {
        private readonly string _target;

        public override string ActionName => "Go To " + _target;
        protected override float StaticCost => 3;

        public GoToAction(string target)
        {
            _target = target;

            Effects.Add("isNear", target);
        }
    }

    public class SellAction : DomainActionBase
    {
        private readonly string _product;
        public override string ActionName => "Sell " + _product;

        public SellAction(string product)
        {
            _product = product;
            Preconditions.Add("isNear", "Shop");
            Preconditions.Add("has" + _product, true);

            Effects.Add("hasGold", true);
            Effects.Add("has" + _product, false);
        }
    }

    public class BuyAction : DomainActionBase
    {
        private readonly string _product;
        public override string ActionName => "Buy " + _product;
        protected override float StaticCost => 2;

        public BuyAction(string product)
        {
            _product = product;

            Preconditions.Add("isNear", "Shop");
            Preconditions.Add("hasGold", true);
            Preconditions.Add("has" + _product, false);

            Effects.Add("has" + _product, true);
            Effects.Add("hasGold", false);
        }
    }

}

    
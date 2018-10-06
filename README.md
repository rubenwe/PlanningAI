# PlanningAI
**G**oal **O**riented **A**ction **P**lanning C#/.NET Library 

This library provides a basic implementation of GOAP with 
a bit of a utility system thrown on top.

[![nuget_version_badge](https://img.shields.io/nuget/v/PlanningAI.png)](https://www.nuget.org/packages/PlanningAI)

## Basics
Goal oriented action planning enables _agents_ to plan a _sequence of actions_ 
to satisfy a specific _goal_. The plan of actions is chosen depending on
the state of the world at the time of planning and the desired goal state.

The state of the world - or `DomainState` as it is called in this project -
is basically just a set of variables. The planner will search through the tree 
of possible actions to find an optimal solution to bring this state in
line with the desired goal state.

## Usage
You can use the system including the agents or just the planning on its own.

The following examples are based show an easy scenario in which 
we want an agent to find a way to fill their belly.

### State
To start planning we need a representation of the current world state 
and the desired end state.

```csharp
var currentState = DomainState.Empty
    .Set("isHungry", true)
    .Set("isAt", "Woods")
    .Set("hasMoney", true);

var goalState = DomainState.Empty
    .Set("isHungry", false);
```

The `DomainState` objects are immutable but use a fluent API for easy composition.

### Actions
Next, we need a definition of what the available actions for an agent are.
You can implement your own actions based on `IDomainAction` or use one of the
provided base classes.

Actions define their preconditions and effects:

```csharp
public class OrderFoodAction : DomainActionBase
{
    // ...
    public OrderFoodAction() 
    {
        Preconditions.Add("isAt", "Tavern");
        Preconditions.Add("hasMoney", true);
        Effects.Add("hasMoney", false);
        Effects.Add("hasFood", true);
    }
}
```

You can use constructor parameters to define more generic actions:

```csharp
public class GoToAction : DomainActionBase
{
    private readonly string _target;
        
    public override string ActionName => "Go To " + _target;
    public GoToAction(string target)
    {
        _target = target;
        Effects.Add("isAt", target);
    }
}
```

### Planner
We create a planner and give it the current state, the goal state and all available actions:

```csharp
// ...
var planner = PlannerFactory.CreatePlanner();
var actions = new List<IDomainAction>
{
    new GoToAction("Tavern"),
    new GoToAction("Woods"),
    new OrderFoodAction(),
    new EatFoodAction()
};

var actionSet = new ActionSet(actions);
var result = planner.GetPlan(currentState, goalState, actionSet);

if(result.Success)
{
    foreach(var action in result.Plan)
    {
        Console.WriteLine(action); // Go To Tavern, Order Food, Eat Food
    }
}

```

#### Debugging the planning
Description coming soon.

### Goals
As mentioned before: If you only need planning, you don't need to define goals.

Goals are a way to determine what actions the planner of an agent should search for.
They allow to inject state into the world when they are activated and they determine
the goal state that must be reached to fulfill them.

```csharp
class FindFoodGoal : AgentGoalBase
{
    public override string GoalName => nameof(FindFoodGoal);
    public override void OnActivation(ref DomainState currentState, ref DomainState goalState)
    {
        currentState = currentState.Set("isHungry", true);
        goalState = DomainState.Empty.Set("isHungry", false);
    }
}
```

In this example we determine that the agent is now hungry and that this goal
will be satisfied if this is no longer the case.

### Executable Actions
Before taking a closer look at agents, we need to "beef up" one of our
previous actions to show how an action can interact with the agent.

```csharp
public interface ICanEat
{
    void Eat();
}

public class OrderFoodAction : AsyncExecutableActionBase
{
    public override string ActionName => "Order Food";
    private readonly ICanEat _eater;
    
    public OrderFoodAction(ICanEat eater) 
    {
        _eater = eater;
        
        Preconditions.Add("isAt", "Tavern");
        Preconditions.Add("hasMoney", true);
        Effects.Add("hasMoney", false);
        Effects.Add("hasFood", true);
    }
    
    public override Task<bool> ExecuteAsync(DomainState currentState, CancellationToken token)
    {
        _eater.Eat();
        return Task.FromResult(true);
    }
}
```

Instead of just using `DomainActionBase` as base type we choose an `AsyncExecutableActionBase`.
This allows us to define the code that will be run once the agent executes the action.

### Agents

A simple way to implement an agent is to inherit from the `Agent` class.
In the following example we have an Agent with ever increasing hunger -
I'm sure you can relate.

The agent has two goals. It can either idle or find food.
The winning goal is determined by evaluating `Consideration`s attached
to the goals.

```csharp
internal class HungryAgent : Agent, ICanEat
{
    private float _hungerLevel;
    public void Eat() => _hungerLevel = 0;

    public void OnTick()
    {
        _hungerLevel = Math.Min(1, _hungerLevel + 0.01f);
    }

    public HungryAgent(IPlanner planner) : base(planner)
    {
        var justIdle = Consideration.FromFunc(() => 0.5f, "Idle");
        var idleGoal = AddGoal<IdleGoal>(1, justIdle);
        AddAction(new IdleAction(TimeSpan.FromSeconds(5)));

        var isHungry = Consideration.FromFunc(() => _hungerLevel, "Hunger");
        var foodGoal = AddGoal<FindFoodGoal>(1, isHungry);
        AddAction(new OrderFoodAction(this));
    }
}
```

As long as the hunger level is low the agent will idle - 
but as soon as the hunger grows too big, it will order something to eat.

#### Agent / Action interaction
How you want to wire up the interactions between the agents and the actions is up to you.
In this example we used an interface and constructor injection. This allows you to unit test actions.
Other alternatives can include callbacks / events or overwriting the `Agent` classes `virtual` 
methods that are provied for these scenarios.

### Binding actions to goals
One of the limiting factors of GOAP is the amount of actions that need to be evaluated.
For this reason and to allow certain actions not to be planned for certain goals
you can bind actions to goals.

```csharp
// Instead of:
AddAction(new OrderFoodAction(this));

// We can create a bindable action and bind it to the goal
var orderFood = AddBindableAction(new OrderFoodAction(this));
orderFood.BindTo(foodGoal);
```

## Dependencies
The current implementation of `DomainState`depends on `System.Collections.Immutable.ImmutableDictionary<,>`.
I'd like to drop this dependency further down the line for easier deployment and better performance.

## Why use this implementation?
There are (a lot of) other implementations of GOAP out there.
Many of them will offer more features and better integration for certain use cases.
This implementation provides a regressive planner, which I personally haven't found 
in other .NET based solutions. 
If you want or need to explore deep and wide, searching from the goal state
can give a significant performance advantage.

Send me a message if you know of other implementations that do it this way -
I would love to take a look :)
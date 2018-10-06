# PlanningAI
**G**oal **O**riented **A**ction **P**lanning C#/.NET Library

This library provides a basic implementation of GOAP with
a bit of a utility system thrown on top.

There are (a lot of) other implementations of GOAP out there.
This implementation provides a regressive planner, 
which I personally haven't found in other .NET based solutions.

Let me know if you find one - I would love to take a look.

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

### Agents
Description coming soon.

## Dependencies
The current implementation of `DomainState`depends on `System.Collections.Immutable.ImmutableDictionary<,>`.
I'd like to drop this dependency further down the line for easier deployment and better performance.

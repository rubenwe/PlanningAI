using PlanningAi.Planning.Actions;

namespace PlanningAi.Planning.Planners
{
    public interface IPlanner
    {
        PlanningResult GetPlan(DomainState startState, DomainState goalState, ActionSet actions);
    }
}
using System.Linq;

namespace PlanningAi.Planning.Actions
{
    public static class DomainActionExtensions
    {
        public static bool HasUnsatisfiedPreconditions(this IDomainAction action, DomainState state)
        {
            return !action.Preconditions.All(pair => state.Fulfills(pair.Key, pair.Value));
        }
    }
}
using System.Collections.Generic;

namespace PlanningAi.Planning.Actions
{
    public abstract class DomainActionBase : IDomainAction
    {
        public virtual bool IsValid => true;
        protected virtual float StaticCost { get; } = 1;
        public abstract string ActionName { get; }

        public Dictionary<string, object> Preconditions { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> Effects { get; } = new Dictionary<string, object>();
        
        public virtual float GetCost(DomainState currentState) => StaticCost;
        
        
        public override string ToString()
        {
            return $"Action: {ActionName}";
        }
    }
}
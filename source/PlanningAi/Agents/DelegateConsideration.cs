using System;
using PlanningAi.Planning;

namespace PlanningAi.Agents
{
    internal sealed class DelegateConsideration : IConsideration
    {
        private readonly Func<DomainState, float> _getValue;
        
        public string Name { get; }

        public DelegateConsideration(string name, Func<DomainState, float> getValue)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _getValue = getValue ?? throw new ArgumentNullException(nameof(getValue));
        }
        
        public float GetValue(DomainState currentState)
        {
            return _getValue(currentState);
        }
    }
}
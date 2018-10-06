using System;

namespace PlanningAi.Agents
{
    internal sealed class DelegateConsideration : IConsideration
    {
        private readonly Func<float> _getValue;
        
        public string Name { get; }

        public DelegateConsideration(string name, Func<float> getValue)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _getValue = getValue ?? throw new ArgumentNullException(nameof(getValue));
        }

        public float GetValue()
        {
            return _getValue();
        }
    }
}
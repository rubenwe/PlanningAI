using System;
using PlanningAi.Planning;

namespace PlanningAi.Agents
{
    internal sealed class StateConsideration : IConsideration
    {
        private readonly string _value;

        public string Name { get; }

        public StateConsideration(string name, string value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _value = string.IsNullOrEmpty(value) ? throw new ArgumentNullException(nameof(value)) : value;
        }

        public float GetValue(DomainState currentState)
        {
            return currentState.GetValueOrDefault<float>(_value);
        }
    }
}

using System;
using JetBrains.Annotations;
using PlanningAi.Planning;

namespace PlanningAi.Agents
{
    [PublicAPI]
    public static class Consideration
    {
        [PublicAPI]
        public static IConsideration FromFunc(Func<float> getValueFunc, string name = null)
        {
            return new DelegateConsideration(name ?? "Unnamed", state => getValueFunc());
        }
        
        [PublicAPI]
        public static IConsideration FromFunc(Func<DomainState, float> getValueFunc, string name = null)
        {
            return new DelegateConsideration(name ?? "Unnamed", getValueFunc);
        }
    }
}
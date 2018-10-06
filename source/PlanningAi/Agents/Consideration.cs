using System;
using JetBrains.Annotations;

namespace PlanningAi.Agents
{
    [PublicAPI]
    public static class Consideration
    {
        [PublicAPI]
        public static IConsideration FromFunc(Func<float> getValueFunc, string name = null)
        {
            return new DelegateConsideration(name ?? "Unnamed", getValueFunc);
        }
    }
}
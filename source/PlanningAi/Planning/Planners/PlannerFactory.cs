using System;
using System.Diagnostics.CodeAnalysis;

namespace PlanningAi.Planning.Planners
{
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Public API")]
    public static class PlannerFactory
    {
        public static IPlanner CreatePlanner(PlannerSettings settings = null)
        {
            settings = settings ?? new PlannerSettings();
            
            var type = settings.PlannerType;
            switch (type)
            {
                case PlannerType.Regressive:
                    return new RegressivePlanner(settings.EarlyExit, settings.CreateDebugGraph);
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(PlannerType), type, null);
            }
        }
    }
}
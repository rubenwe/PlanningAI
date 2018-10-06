using JetBrains.Annotations;

namespace PlanningAi.Planning.Planners
{
    [PublicAPI]
    public class PlannerSettings
    {
        public PlannerType PlannerType { get; set; } = PlannerType.Regressive;
        public bool EarlyExit { get; set; } = true;
        public bool CreateDebugGraph { get; set; }
    }
}
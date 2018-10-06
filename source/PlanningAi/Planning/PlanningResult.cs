using System.Collections.Generic;
using PlanningAi.Planning.Actions;
using PlanningAi.Planning.Utils;

namespace PlanningAi.Planning
{
    public class PlanningResult
    {
        public bool Success => Plan != null;
        public IReadOnlyList<IDomainAction> Plan { get; internal set; }
        public IReadOnlyGraph<IPlanNode, IDomainAction> SearchTree { get; internal set; }
    }
}
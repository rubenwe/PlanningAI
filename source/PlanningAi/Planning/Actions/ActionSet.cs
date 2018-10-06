using System.Collections.Generic;

namespace PlanningAi.Planning.Actions
{
    public sealed class ActionSet
    {
        private readonly Dictionary<string, List<(IDomainAction action, object effectValue)>> _lookup 
            = new Dictionary<string, List<(IDomainAction action, object effectValue)>>();

        public ActionSet(IEnumerable<IDomainAction> actions)
        {
            foreach (var action in actions)
            foreach (var pair in action.Effects)
            {
                if (!_lookup.TryGetValue(pair.Key, out var list))
                {
                    list = new List<(IDomainAction, object)>();
                    _lookup.Add(pair.Key, list);
                }

                list.Add((action, pair.Value));
            }
        }

        public bool TryGetOut(string worldVar, out IReadOnlyList<(IDomainAction action, object effectValue)> matchingActions)
        {
            if (_lookup.TryGetValue(worldVar, out var actions))
            {
                matchingActions = actions;
                return true;
            }

            matchingActions = null;
            return false;
        }
    }
}
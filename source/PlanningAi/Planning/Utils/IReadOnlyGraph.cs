using System.Collections.Generic;
using JetBrains.Annotations;

namespace PlanningAi.Planning.Utils
{
    [PublicAPI]
    public interface IReadOnlyGraph<out TNode, out TEdgeValue>
    {
        IReadOnlyCollection<TNode> Nodes { get; }
        IReadOnlyCollection<IEdge<TNode, TEdgeValue>> Edges { get; }
    }
}
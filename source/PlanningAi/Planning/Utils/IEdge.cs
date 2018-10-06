using JetBrains.Annotations;

namespace PlanningAi.Planning.Utils
{
    [PublicAPI]
    public interface IEdge<out TNode, out TValue>
    {
        TNode Target { get; }
        TValue Value { get; }
        TNode Source { get; }
    }
}
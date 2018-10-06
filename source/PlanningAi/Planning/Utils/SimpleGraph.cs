using System;
using System.Collections.Generic;

namespace PlanningAi.Planning.Utils
{
    internal class SimpleGraph<TNode, TEdgeValue>  : IReadOnlyGraph<TNode, TEdgeValue>
        where TNode : class
    {
        private readonly HashSet<TNode> _nodes = new HashSet<TNode>();
        private readonly List<Edge> _edges = new List<Edge>();
		
        public IReadOnlyCollection<TNode> Nodes => _nodes;
        public IReadOnlyCollection<IEdge<TNode, TEdgeValue>> Edges => _edges;

        private sealed class Edge : IEdge<TNode, TEdgeValue>
        {
            private bool Equals(Edge other)
            {
                return EqualityComparer<TNode>.Default.Equals(Source, other.Source) 
                       && EqualityComparer<TNode>.Default.Equals(Target, other.Target) 
                       && EqualityComparer<TEdgeValue>.Default.Equals(Value, other.Value);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj.GetType() == GetType() && Equals((Edge) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = EqualityComparer<TNode>.Default.GetHashCode(Source);
                    hashCode = (hashCode * 397) ^ EqualityComparer<TNode>.Default.GetHashCode(Target);
                    hashCode = (hashCode * 397) ^ EqualityComparer<TEdgeValue>.Default.GetHashCode(Value);
                    return hashCode;
                }
            }

            public TNode Source { get; }
            public TNode Target { get; }
            public TEdgeValue Value { get; }

            public Edge(TNode source, TNode target, TEdgeValue value)
            {
                Source = source ?? throw new ArgumentNullException(nameof(source));
                Target = target ?? throw new ArgumentNullException(nameof(target));
                Value = value;
            }
        }

        public void AddNode(TNode node)
        {
            _nodes.Add(node);
        }

        public void AddEdge(TNode source, TNode target, TEdgeValue value)
        {
            _edges.Add(new Edge(source, target, value));
        }
    }
}
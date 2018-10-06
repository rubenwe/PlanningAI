using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace PlanningAi.Planning
{
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Public API")]
    public sealed class DomainState : IEquatable<DomainState>
    {
        private readonly ImmutableDictionary<string, object> _state;
        private readonly Lazy<int> _hashCode;

        public static DomainState Empty { get; } = new DomainState();

        private DomainState(ImmutableDictionary<string, object> state = null)
        {
            _state = state ?? ImmutableDictionary<string, object>.Empty;
            _hashCode = new Lazy<int>(ComputeHashCode);
        }

        private int ComputeHashCode()
        {
            unchecked
            {
                var hash = 17;
                foreach (var pair in _state)
                {
                    hash = hash * 23 + (pair.Key?.GetHashCode() ?? 47);
                    hash = hash * 23 + (pair.Value?.GetHashCode() ?? 269);
                }
                
                return hash;
            }
        }

        public T GetValueOrDefault<T>(string itemName, T defaultValue = default)
        {
            return TryGet(itemName, out T item) ? item : defaultValue;
        }

        public bool TryGet<T>(string itemName, out T item) 
        {
            if (_state.TryGetValue(itemName, out var obj))
            {
                item = (T) obj;
                return true;
            }

            item = default;
            return false;
        }

        public DomainState Set<T>(string itemName, T value) 
        {
            return new DomainState(_state.SetItem(itemName, value));
        }

        public DomainState SetRange(IEnumerable<KeyValuePair<string, object>> values)
        {
            return new DomainState(_state.SetItems(values));
        }
        
        public DomainState Remove(string itemName)
        {
            return new DomainState(_state.Remove(itemName));
        }
                
        public IEnumerable<(string, object)> GetAll()
        {
            foreach (var pair in _state)
            {
                yield return (pair.Key, pair.Value);
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            
            return obj is DomainState state && Equals(state);
        }

        public bool Fulfills(string worldVar, object value)
        {
            var containsState = _state.TryGetValue(worldVar, out var item);

            return containsState && Equals(item, value)
                   || !containsState && IsDefault(value);
        }
        
        public bool IsSubStateOf(DomainState other)
        {
            if (other == null) return false;
            if (Equals(other)) return true;

            var otherState = other._state;
            foreach (var pair in _state)
            {
                if (otherState.TryGetValue(pair.Key, out var otherValue))
                {
                    if (!pair.Value.Equals(otherValue)) return false;
                }
                else if(!IsDefault(pair.Value)) return false;
            }

            return true;
        }

        private static bool IsDefault(object value)
        {
            switch (value)
            {
                case bool b when b == default:
                case int  i when i == default:
                    return true;
                default:
                    return ReferenceEquals(value, null);
            }
        }

        public float DistanceTo(DomainState other, bool oneWay = false)
        {
            var distance = 0f;
            var sameKeys = 0;
            
            foreach (var pair in _state)
            {
                if (other._state.TryGetValue(pair.Key, out var otherValue))
                {
                    sameKeys++;
                    if (!pair.Value.Equals(otherValue))
                    {
                        distance += ComputeDistance(pair.Value, otherValue);
                    }
                }
                else
                {
                    distance += 1;
                }
            }

            if (!oneWay)
            {
                distance += other._state.Count - sameKeys;
            }
            
            return distance;
        }

        private static float ComputeDistance(object value1, object value2)
        {
            switch (value1)
            {
                case int i1 when value2 is int i2:
                    return (float) Math.Abs(i1 - i2) / Math.Max(i1, i2);
                case float f1 when value2 is float f2:
                    return Math.Abs(f1 - f2) / Math.Max(f1, f2);
                case long l1 when value2 is long l2:
                    return (float) Math.Abs(l1 - l2) / Math.Max(l1, l2);
                case double d1 when value2 is double d2:
                    return (float) (Math.Abs(d1 - d2) / Math.Max(d1, d2));
                case decimal m1 when value2 is decimal m2:
                    return  (float) (Math.Abs(m1 - m2) / Math.Max(m1, m2));
            }
            
            return 1;
        }

        public override int GetHashCode() => _hashCode.Value;
        public bool Equals(DomainState other) => other != null && other._hashCode.Value == _hashCode.Value;
        public static bool operator ==(DomainState left, DomainState right) => Equals(left, right);
        public static bool operator !=(DomainState left, DomainState right) => !Equals(left, right);

        public bool IsSuperstateOf(DomainState other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            return other.IsSubStateOf(this);
        }

        public override string ToString()
        {
            return "{" + string.Join(", ", _state.Select(pair => $"({pair.Key}: {pair.Value})")) + "}";
        }

        public DomainState Apply(Dictionary<string, object> effects)
        {
            var state = this;
            foreach (var pair in effects)
            {
                state = state.Set(pair.Key, pair.Value);
            }

            return state;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace PlanningAi.Utils
{
    [PublicAPI]
    public class ResourcePool<T>  
    {
        private static readonly Rentable[] EmptyArray = new Rentable[0];
        private readonly AsyncAutoResetEvent _lock = new AsyncAutoResetEvent();
        private Rentable[] _rentables;

        public int ItemCount { get { lock (_lock) return _rentables.Length; } }

        public ResourcePool()
        {
            _rentables = EmptyArray;
        }
        
        public ResourcePool(IEnumerable<T> items)
        {
            _rentables = items
                .Select(item => new Rentable(item))
                .ToArray();
        }
        
        public async Task<T> RentAsync(CancellationToken token = default)
        {
            return await RentAsync(null, null, token);
        }
        
        public async Task<T> RentAsync(Func<T, bool> canRent, CancellationToken token = default)
        {
            return await RentAsync(canRent, null, token);
        }
        
        public async Task<T> RentAsync(Func<T, float> getCost, CancellationToken token = default)
        {
            return await RentAsync(null, getCost, token);
        }

        public async Task<T> RentAsync(
            Func<T, bool> canRent, 
            Func<T, float> getCost, 
            CancellationToken token = default)
        {
            if (token.IsCancellationRequested) return default;
            canRent = canRent ?? AlwaysTrue;
            
            if (TryRent(canRent, getCost, out var rented))
            {
                return rented;
            }

            return await WaitForMatchingItem(canRent, getCost, token);
        }

        public void Return(T item)
        {
            lock (_lock)
            {
                for (var i = 0; i < _rentables.Length; i++)
                {
                    ref var rentable = ref _rentables[i];
                    if (!ReferenceEquals(rentable.Item, item)) continue;

                    rentable.IsFree = true;
                    break;
                }
            }
            
            _lock.Set();
        }

        public void Add(T item)
        {
            lock (_lock)
            {
                var oldRentables = _rentables;
                _rentables = new Rentable[_rentables.Length + 1];
                
                Array.Copy(oldRentables, _rentables, oldRentables.Length);
                
                _rentables[oldRentables.Length] = new Rentable(item);
            }
        }
        
        public void AddRange(IReadOnlyList<T> items)
        {
            lock (_lock)
            {
                var oldRentables = _rentables;
                _rentables = new Rentable[_rentables.Length + items.Count];
                
                Array.Copy(oldRentables, _rentables, oldRentables.Length);
                
                for (var i = 0; i < items.Count; i++)
                {
                    _rentables[oldRentables.Length + i] = new Rentable(items[i]);
                }
            }
        }
        
        private bool AlwaysTrue(T arg) => true;

        private bool TryRent(Func<T, bool> canRent, Func<T, float> getCost, out T rented)
        {
            rented = default;
            
            lock (_lock)
            {
                if (getCost == null)
                {
                    for (var i = 0; i < _rentables.Length; i++)
                    {
                        ref var rentable = ref _rentables[i];
                        if (!rentable.IsFree || !canRent(rentable.Item)) continue;

                        return Rent(out rented, ref rentable);
                    }
                }
                else
                {
                    var minCost = float.MaxValue;
                    var minIdx = -1;
                    for (var i = 0; i < _rentables.Length; i++)
                    {
                        ref var rentable = ref _rentables[i];
                        if (!rentable.IsFree || !canRent(rentable.Item)) continue;
                        
                        var cost = getCost(rentable.Item);
                        if (cost > minCost) continue;
                        
                        minCost = cost;
                        minIdx = i;
                    }

                    if (minIdx == -1) return false;
                    ref var result = ref _rentables[minIdx];
                    
                    return Rent(out rented, ref result);
                }
                
            }
            
            return false;
        }

        private bool Rent(out T rented, ref Rentable rentable)
        {
            rented = rentable.Item;
            rentable.IsFree = false;
            
            return true;
        }

        private async Task<T> WaitForMatchingItem(
            Func<T, bool> canRent, 
            Func<T, float> getPriority,
            CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await _lock.WaitAsync(token);
                if (TryRent(canRent, getPriority, out var rented))
                {
                    return rented;
                }
            }

            return default;
        }
        
        private struct Rentable
        {
            public bool IsFree;
            public readonly T Item;

            public Rentable(T item)
            {
                Item = item;
                IsFree = true;
            }
        }
    }
}
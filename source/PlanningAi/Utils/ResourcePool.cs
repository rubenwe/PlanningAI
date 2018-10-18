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
        private readonly Rentable[] _rentables;
        private readonly object _syncRoot = new object();
        private readonly AsyncAutoResetEvent _lock = new AsyncAutoResetEvent();

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

        private bool AlwaysTrue(T arg) => true;

        private bool TryRent(Func<T, bool> canRent, Func<T, float> getCost, out T rented)
        {
            bool IsCandidate(ref Rentable rentable)
            {
                return rentable.IsFree && canRent(rentable.Item);
            }

            rented = default;
            
            lock (_syncRoot)
            {
                if (getCost == null)
                {
                    for (var i = 0; i < _rentables.Length; i++)
                    {
                        ref var rentable = ref _rentables[i];
                        if (!IsCandidate(ref rentable)) continue;

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
                        if (!IsCandidate(ref rentable)) continue;
                        
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

        public void Return(T item)
        {
            lock (_syncRoot)
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
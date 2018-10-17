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

        public async Task<T> RentAsync(Func<T, bool> canRent, CancellationToken token = default)
        {
            if (token.IsCancellationRequested) return default;

            if (TryRent(canRent, out var rentAsync))
            {
                return rentAsync;
            }

            return await WaitForMatchingItem(canRent, token);
        }

        private bool TryRent(Func<T, bool> canRent, out T rented)
        {
            lock (_syncRoot)
            {
                rented = default;
            
                for (var i = 0; i < _rentables.Length; i++)
                {
                    ref var rentable = ref _rentables[i];
                    if (!rentable.IsFree || !canRent(rentable.Item)) continue;

                    rentable.IsFree = false;
                    rented = rentable.Item;
                
                    return true;
                }

                return false;
            }
        }

        private async Task<T> WaitForMatchingItem(Func<T, bool> canRent, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await _lock.WaitAsync(token);
                if (TryRent(canRent, out var rented))
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
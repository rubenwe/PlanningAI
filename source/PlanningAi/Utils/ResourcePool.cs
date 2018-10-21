using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace PlanningAi.Utils
{
    [PublicAPI]
    public class ResourcePool<T> : IResourcePool<T>
    {
        private static readonly Rentable[] EmptyArray = new Rentable[0];
        private readonly AsyncAutoResetEvent _lock = new AsyncAutoResetEvent();
        private Rentable[] _rentables;

        public int ItemCount { get { lock (_lock) return _rentables.Length; } }

        public ResourcePool()
        {
            _rentables = EmptyArray;
        }

        public ResourcePool(params T[] items) 
            : this(items.AsEnumerable())
        {
        }
        
        public ResourcePool(IEnumerable<T> items)
        {
            _rentables = items
                .Select(item => new Rentable(item))
                .ToArray();
        }
        
        public Task<T> RentAsync(CancellationToken token = default)
        {
            return RentAsync(null, null, token);
        }

        public Task<T> RentAsync(object owner, CancellationToken token = default)
        {
            return RentAsync(owner, null, null, token);
        }
        
        public Task<T> RentAsync(Func<T, bool> canRent, CancellationToken token = default)
        {
            return RentAsync(canRent, null, token);
        }
        
        public Task<T> RentAsync(object owner, Func<T, bool> canRent, CancellationToken token = default)
        {
            return RentAsync(owner, canRent, null, token);
        }
        
        public Task<T> RentAsync(Func<T, float> getCost, CancellationToken token = default)
        {
            return RentAsync(null, getCost, token);
        }
        
        public Task<T> RentAsync(object owner, Func<T, float> getCost, CancellationToken token = default)
        {
            return RentAsync(owner, null, getCost, token);
        }

        public Task<T> RentAsync(
            Func<T, bool> canRent,
            Func<T, float> getCost,
            CancellationToken token = default)
        {
            return RentAsync(null, canRent, getCost, token);
        }

        public async Task<T> RentAsync(
            object owner,
            Func<T, bool> canRent, 
            Func<T, float> getCost, 
            CancellationToken token = default)
        {
            if (token.IsCancellationRequested) return default;
            canRent = canRent ?? AlwaysTrue;
            
            if (TryRent(owner, canRent, getCost, out var rented))
            {
                return rented;
            }

            return await WaitForMatchingItem(owner, canRent, getCost, token);
        }

        public void Return(T item)
        {
            Return(null, item);
        }
        
        public void Return(object owner, T item)
        {
            var returned = false;
            lock (_lock)
            {
                for (var i = 0; i < _rentables.Length; i++)
                {
                    ref var rentable = ref _rentables[i];
                    if (!Equals(rentable.Item, item)) continue;

                    if (rentable.IsFree) ThrowHelper.ResourceNotRented(rentable.Item);
                    if (!Equals(rentable.Owner, owner)) ThrowHelper.WrongOwner(rentable, owner);
                    
                    rentable.IsFree = true;
                    rentable.Owner = null;
                    returned = true;
                    
                    break;
                }
            }

            if (!returned) ThrowHelper.ResourceNotInPool(item);
            
            _lock.Set();
        }
        
        internal void ReturnAll(object owner)
        {
            var returned = 0;
            lock (_lock)
            {
                for (var i = 0; i < _rentables.Length; i++)
                {
                    ref var rentable = ref _rentables[i];
                    if (rentable.IsFree || !Equals(rentable.Owner, owner)) continue;

                    rentable.IsFree = true;
                    rentable.Owner = null;
                    returned++;
                }
            }

            for (var i = 0; i < returned; i++)
            {
                _lock.Set();
            }
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
        
        public bool IsRented(T item)
        {
            lock (_lock)
            {
                for (var i = 0; i < _rentables.Length; i++)
                {
                    ref var rentable = ref _rentables[i];
                    if (!Equals(rentable.Item, item)) continue;

                    return !rentable.IsFree;
                }
            }

            return ThrowHelper.ResourceNotInPool(item);
        }

        public IPoolOwnerView<T> GetViewForOwner(object owner)
        {
            return new PoolOwnerView<T>(this, owner);
        }
        
        private bool AlwaysTrue(T arg) => true;

        private bool TryRent(object owner, Func<T, bool> canRent, Func<T, float> getCost, out T rented)
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

                        return Rent(owner, out rented, ref rentable);
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
                    
                    return Rent(owner, out rented, ref result);
                }
                
            }
            
            return false;
        }

        private bool Rent(object owner, out T rented, ref Rentable rentable)
        {
            rented = rentable.Item;
            rentable.IsFree = false;
            rentable.Owner = owner;
            
            return true;
        }

        private async Task<T> WaitForMatchingItem(
            object owner,
            Func<T, bool> canRent, 
            Func<T, float> getPriority,
            CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await _lock.WaitAsync(token);
                if (TryRent(owner, canRent, getPriority, out var rented))
                {
                    return rented;
                }
                
                _lock.Set();
            }

            return default;
        }
        
        private struct Rentable
        {
            public volatile object Owner;
            public volatile bool IsFree;
            public readonly T Item;

            public Rentable(T item)
            {
                Item = item;
                Owner = null;
                IsFree = true;
            }
            
            public override string ToString()
            {
                return $"{nameof(Item)}: {Item}, {nameof(IsFree)}: {IsFree}, {nameof(Owner)}: {Owner}";
            }
        }
        
        private class ThrowHelper
        {
            public static void WrongOwner(Rentable rentable, object returnee)
            {
                throw new InvalidOperationException(
                    $"Resource `{rentable.Item}` can't be returned by owner {returnee}." +
                    $" It is owned by {rentable.Owner ?? "an anonymous owner"}.");
            }

            public static void ResourceNotRented(T item)
            {
                throw new InvalidOperationException(
                    $"Resource `{item}` can't be returned because it is not currently rented!");
            }

            public static bool ResourceNotInPool(T item)
            {
                throw new ArgumentException(
                    $"Resource `{item}` is not managed by this pool.");
            }
        }
    }
}
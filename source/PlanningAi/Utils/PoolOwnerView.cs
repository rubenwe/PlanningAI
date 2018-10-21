using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace PlanningAi.Utils
{
    internal sealed class PoolOwnerView<T> : IPoolOwnerView<T>
    {
        private readonly ResourcePool<T> _resourcePool;
        private readonly object _owner;

        public PoolOwnerView([NotNull] ResourcePool<T> resourcePool, [NotNull] object owner)
        {
            _resourcePool = resourcePool ?? throw new ArgumentNullException(nameof(resourcePool));
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public Task<T> RentAsync(CancellationToken token = default)
        {
            return _resourcePool.RentAsync(_owner, token);
        }

        public Task<T> RentAsync(Func<T, bool> canRent, CancellationToken token = default)
        {
            return _resourcePool.RentAsync(_owner, canRent, token);
        }

        public Task<T> RentAsync(Func<T, float> getCost, CancellationToken token = default)
        {
            return _resourcePool.RentAsync(_owner, getCost, token);
        }

        public Task<T> RentAsync(Func<T, bool> canRent, Func<T, float> getCost, CancellationToken token = default)
        {
            return _resourcePool.RentAsync(_owner, canRent, getCost, token);
        }

        public void Return(T item)
        {
            _resourcePool.Return(_owner, item);
        }

        public void ReturnAll()
        {
            _resourcePool.ReturnAll(_owner);
        }

        public void Dispose()
        {
            ReturnAll();
        }
    }
}
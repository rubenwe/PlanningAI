using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace PlanningAi.Utils
{
    [PublicAPI]
    public interface IResourcePool<T>
    {
        Task<T> RentAsync(CancellationToken token = default);
        Task<T> RentAsync(Func<T, bool> canRent, CancellationToken token = default);
        Task<T> RentAsync(Func<T, float> getCost, CancellationToken token = default);
        Task<T> RentAsync(Func<T, bool> canRent, Func<T, float> getCost, CancellationToken token = default);

        void Return(T item);
    }
}
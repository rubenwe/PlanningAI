using System;
using JetBrains.Annotations;

namespace PlanningAi.Utils
{
    [PublicAPI]
    public interface IPoolOwnerView<T> : IResourcePool<T>, IDisposable
    {
        void ReturnAll();
    }
}
using System.Threading;
using System.Threading.Tasks;

namespace PlanningAi.Planning.Actions
{
    public interface IAsyncExecutableAction : IDomainAction
    {
        Task<bool> ExecuteAsync(DomainState currentState, CancellationToken token = default);
    }
}
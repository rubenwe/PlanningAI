using System.Threading;
using System.Threading.Tasks;

namespace PlanningAi.Planning.Actions
{
    public abstract class AsyncExecutableActionBase : DomainActionBase, IAsyncExecutableAction
    {
        public abstract Task<bool> ExecuteAsync(DomainState currentState, CancellationToken token);
    }
}
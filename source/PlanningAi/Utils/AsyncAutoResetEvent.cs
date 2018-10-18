using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanningAi.Utils
{
    public sealed class AsyncAutoResetEvent
    {
        private readonly object _syncRoot = new object();
        private readonly Queue<TaskCompletionSource<bool>> _taskQueue = new Queue<TaskCompletionSource<bool>>();
        
        public async Task WaitAsync(CancellationToken token = default)
        {
            if (token.IsCancellationRequested) return;
            
            var source = new TaskCompletionSource<bool>();
            if (token.CanBeCanceled)
            {
                token.Register(() =>
                {
                    if (source.Task.IsCompleted) return;
                    source.SetCanceled();
                });
            }
            
            lock (_syncRoot)
            {
                _taskQueue.Enqueue(source);
            }

            await source.Task;
        }

        public void Set()
        {
            TaskCompletionSource<bool> source;
            while ((source = GetNextQueueEntry()) != null)
            {
                if (source.Task.IsCompleted) continue;
                
                source.SetResult(true);
                return;
            }
        }

        private TaskCompletionSource<bool> GetNextQueueEntry()
        {
            lock (_syncRoot)
            {
                return _taskQueue.Count != 0 
                    ? _taskQueue.Dequeue() 
                    : null;
            }
        }
    }
}
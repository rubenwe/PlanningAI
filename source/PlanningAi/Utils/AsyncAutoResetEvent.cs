using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanningAi.Utils
{
    public sealed class AsyncAutoResetEvent
    {
        private readonly Queue<TaskCompletionSource<bool>> _taskQueue = new Queue<TaskCompletionSource<bool>>();
        private readonly Task _completed = Task.CompletedTask;
        
        public Task WaitAsync(CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
            {
                return _completed;
            }
            
            var source = new TaskCompletionSource<bool>();
            if (token.CanBeCanceled)
            {
                token.Register(() =>
                {
                    if (source.Task.IsCompleted) return;
                    source.SetCanceled();
                });
            }
            
            lock (_taskQueue)
            {
                _taskQueue.Enqueue(source);
            }

            return source.Task;
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
            lock (_taskQueue)
            {
                return _taskQueue.Count != 0 
                    ? _taskQueue.Dequeue() 
                    : null;
            }
        }
    }
}
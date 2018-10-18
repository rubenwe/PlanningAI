using System.Threading;
using System.Threading.Tasks;
using PlanningAi.Utils;
using Xunit;

namespace PlanningAI.Tests
{
    public class AsyncAutoResetEventTests
    {
        [Fact]
        public void ShouldReleaseOneWaitingTask()
        {
            var evt = new AsyncAutoResetEvent();

            var task1 = evt.WaitAsync();
            var task2 = evt.WaitAsync();
            
            evt.Set();
            
            Assert.True(task1.IsCompletedSuccessfully);
            Assert.False(task2.IsCompleted);
        }

        [Fact]
        public void ShouldReleaseFirstNonCanceledTask()
        {
            var source = new CancellationTokenSource();
            var evt = new AsyncAutoResetEvent();

            var task1 = evt.WaitAsync(source.Token);
            var task2 = evt.WaitAsync(default);
            
            source.Cancel();
            evt.Set();
            
            Assert.True(task1.IsCanceled);
            Assert.True(task2.IsCompletedSuccessfully);
        }
        
        [Fact]
        public async Task AwaitingCanceledTaskShouldThrow()
        {
            var evt = new AsyncAutoResetEvent();
            
            var source = new CancellationTokenSource();
            var exTask = Assert.ThrowsAsync<TaskCanceledException>(async () => await evt.WaitAsync(source.Token));
            
            source.Cancel();

            await exTask;
        }
    }
}
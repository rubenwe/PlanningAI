using System.Threading.Tasks;

namespace PlanningAi.Utils
{
    public static class TaskExtensions
    {
        public static bool IsCompletedSuccessfully(this Task task)
        {
            return task.IsCompleted && !task.IsCanceled && !task.IsFaulted;
        }
    }
}
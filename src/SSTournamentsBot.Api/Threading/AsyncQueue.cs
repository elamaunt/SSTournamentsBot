using System;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.Threading
{
    public class AsyncQueue
    {
        readonly ConcurrentExclusiveSchedulerPair _pair = new ConcurrentExclusiveSchedulerPair();
        private TaskScheduler Exclusive => _pair.ExclusiveScheduler;

        public Task Async(Action action)
        {
            var task = new Task(action, TaskCreationOptions.PreferFairness | TaskCreationOptions.RunContinuationsAsynchronously | TaskCreationOptions.HideScheduler);
            task.Start(Exclusive);
            return task;
        }

        public Task<T> Async<T>(Func<T> func)
        {
            var task = new Task<T>(func, TaskCreationOptions.PreferFairness | TaskCreationOptions.RunContinuationsAsynchronously | TaskCreationOptions.HideScheduler);
            task.Start(Exclusive);
            return task;
        }
    }
}

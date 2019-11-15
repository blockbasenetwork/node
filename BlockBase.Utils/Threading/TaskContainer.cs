using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlockBase.Utils.Threading
{
    public class TaskContainer
    {
        public string TaskIdentifier { get; set; }
        public Task Task { get; set; }
        public Action Action { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }

        public static TaskContainer Create(Action task, string taskIdentifier = null)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            return new TaskContainer
            {
                TaskIdentifier = taskIdentifier,
                CancellationTokenSource = cancellationTokenSource,
                Action = task
            };
        }

        public void Start()
        {
            Task = Task.Run(Action, CancellationTokenSource.Token);
        }

        public void Stop()
        {
            CancellationTokenSource.Cancel();
        }
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlockBase.Utils.Threading
{
    public class TaskContainer
    {
        public string TaskIdentifier { get; set; }
        public Task Task { get; set; }
        public Func<Task> Func { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }

        public static TaskContainer Create(Func<Task> func, string taskIdentifier = null)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            return new TaskContainer
            {
                TaskIdentifier = taskIdentifier,
                CancellationTokenSource = cancellationTokenSource,
                Func = func
            };
        }

        public void Start()
        {
            Task = Task.Run(Func, CancellationTokenSource.Token);
        }

        //TODO rpinto - consider doing an assynchronous version
        public void Stop()
        {
            CancellationTokenSource.Cancel();
            
            //should it wait for the task to cancel? It was getting stuck on the waiting process...
            //Task.Wait();
        }
    }
}
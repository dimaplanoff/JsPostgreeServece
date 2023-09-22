

namespace SLScheduler
{
    public class SLSchedulerWorker : BackgroundService
    {
        private Task[] tasks = new Task[Const.Data.Tasks.Length];
        private int i = 0;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var task in Const.Data.Tasks)            
                tasks[i++] = Task.Factory.StartNew(async () => 
                    await task.StartAsync(stoppingToken), 
                    TaskCreationOptions.LongRunning 
                        | TaskCreationOptions.RunContinuationsAsynchronously 
                        | TaskCreationOptions.AttachedToParent);

            await Task.WhenAny(tasks);
        }
    }
}

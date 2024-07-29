namespace Common;

internal static class TaskQueue
{
    public static async Task RunAsync(IEnumerable<Func<Task>> tasks, int maxParallel)
    {
        var semaphoreSlim = new SemaphoreSlim(maxParallel, maxParallel); // Limit to maxParallel concurrent tasks with a max queue size of maxParallel

        var runningTasks = new List<Task>();
        foreach (var task in tasks)
        {
            await semaphoreSlim.WaitAsync(); // Wait until a slot becomes available
            try
            {
                var runningTask = task()
                .ContinueWith(_ =>
                {
                    runningTasks.Remove(_);
                    semaphoreSlim.Release(); // Release the slot after the task finishes
                });

                runningTasks.Add(runningTask);
            }
            catch
            {
                // Handle exceptions appropriately
                semaphoreSlim.Release(); // Release the slot even on exception
            }
        }

        await Task.WhenAll(runningTasks); // Wait for all currently running tasks to finish
    }
}
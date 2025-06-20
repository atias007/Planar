using System;
using System.Threading.Tasks;

namespace Planar.Job
{
    public interface IBaseJob
    {
        IJobExecutionContext Context { get; }

        TimeSpan JobRunTime { get; }

        Task AddAggregateExceptionAsync(Exception ex, int maxItems = 25);

        int ExceptionCount { get; }

        int? EffectedRows { get; }

        Task IncreaseEffectedRowsAsync(int value = 1);

        Task SetEffectedRowsAsync(int value);

        DateTime Now();

        Task PutJobDataAsync(string key, object value);

        Task PutTriggerDataAsync(string key, object value);

        Task RemoveJobDataAsync(string key);

        Task RemoveTriggerDataAsync(string key);

        Task ClearJobDataAsync();

        Task ClearTriggerDataAsync();

        Task UpdateProgressAsync(long current, long total);

        Task UpdateProgressAsync(byte value);

        Task RaiseCustomEventAsync(CustomMonitorEvents customMonitorEvents, string message);

#if NETSTANDARD2_0

        Task InvokeJobAsync(string id, InvokeJobOptions options = null);

#else
        Task InvokeJobAsync(string id, InvokeJobOptions? options = null);
#endif

#if NETSTANDARD2_0

        Task QueueInvokeJobAsync(string id, DateTime dueDate, InvokeJobOptions options = null);

#else
        Task QueueInvokeJobAsync(string id, DateTime dueDate, InvokeJobOptions? options = null);
#endif
    }
}
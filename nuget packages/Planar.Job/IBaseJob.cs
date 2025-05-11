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
    }
}
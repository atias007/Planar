using System;
using System.Threading.Tasks;

namespace Planar.Job
{
    public interface IBaseJob
    {
        IJobExecutionContext Context { get; }

        TimeSpan JobRunTime { get; }

        void AddAggregateException(Exception ex, int maxItems = 25);

        Task AddAggregateExceptionAsync(Exception ex, int maxItems = 25);

        void CheckAggragateException();

        int ExceptionCount { get; }

        int? EffectedRows { get; set; }

        DateTime Now();

        void PutJobData(string key, object value);

        Task PutJobDataAsync(string key, object value);

        void PutTriggerData(string key, object value);

        Task PutTriggerDataAsync(string key, object value);

        void RemoveJobData(string key);

        Task RemoveJobDataAsync(string key);

        void RemoveTriggerData(string key);

        Task RemoveTriggerDataAsync(string key);

        void ClearJobData();

        Task ClearJobDataAsync();

        void ClearTriggerData();

        Task ClearTriggerDataAsync();

        void UpdateProgress(byte value);

        void UpdateProgress(long current, long total);

        Task UpdateProgressAsync(long current, long total);

        Task UpdateProgressAsync(byte value);
    }
}
using System;

namespace Planar.Job
{
    public interface IBaseJob
    {
        IJobExecutionContext Context { get; }

        TimeSpan JobRunTime { get; }

        void AddAggregateException(Exception ex, int maxItems = 25);

        void CheckAggragateException();

        int ExceptionCount { get; }

        int? EffectedRows { get; set; }

        DateTime Now();

        void PutJobData(string key, object value);

        void PutTriggerData(string key, object value);

        void UpdateProgress(byte value);

        void UpdateProgress(int current, int total);
    }
}
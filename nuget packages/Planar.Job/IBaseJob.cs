using System;

namespace Planar.Job
{
    public interface IBaseJob
    {
        IJobExecutionContext Context { get; }

        TimeSpan JobRunTime { get; }

        void AddAggregateException(Exception ex);

        void CheckAggragateException();

        int? GetEffectedRows();

        void IncreaseEffectedRows(int delta = 1);

        DateTime Now();

        void PutJobData(string key, object value);

        void PutTriggerData(string key, object value);

        void SetEffectedRows(int value);

        void UpdateProgress(byte value);

        void UpdateProgress(int current, int total);
    }
}
using System;

namespace Planar.Job
{
    public interface IBaseJob
    {
        TimeSpan JobRunTime { get; }

        void AddAggregateException(Exception ex);

        void CheckAggragateException();

        bool CheckIfStopRequest();

        void FailOnStopRequest(Action? stopHandle = default);

        T GetData<T>(string key);

        string GetData(string key);

        int? GetEffectedRows();

        void IncreaseEffectedRows(int delta = 1);

        bool IsDataExists(string key);

        DateTime Now();

        void PutJobData(string key, object value);

        void PutTriggerData(string key, object value);

        void SetEffectedRows(int value);

        void UpdateProgress(byte value);

        void UpdateProgress(int current, int total);
    }
}
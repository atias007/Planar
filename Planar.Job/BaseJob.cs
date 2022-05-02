using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Planar.Job
{
    public abstract class BaseJob
    {
        private readonly object Locker = new();
        private JobExecutionMetadata _metadata;
        private JobExecutionContext _context;
        private bool? _isNowOverrideValueExists;
        private DateTime? _nowOverrideValue;
        private Dictionary<string, string> JobSettings { get; set; } = new();

        public Task Execute(string context, string settings, ref object state)
        {
            _metadata = new JobExecutionMetadata();

            // TODO: check for deserialize error
            _context = JsonSerializer.Deserialize<JobExecutionContext>(context);
            _context.State = state;

            // TODO: check for deserialize error
            if (settings != null)
            {
                JobSettings = JsonSerializer.Deserialize<Dictionary<string, string>>(settings);
            }

            return ExecuteJob(_context)
                .ContinueWith(t =>
                {
                    if (t.Exception != null) { throw t.Exception; }
                });
        }

        public abstract Task ExecuteJob(JobExecutionContext context);

        protected void AddAggragateException(Exception ex)
        {
            lock (Locker)
            {
                _metadata.Exceptions.Add(new ExceptionDto(ex));
            }
        }

        protected void AppendInformation(string info)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Out.WriteLineAsync(info);
            Console.ForegroundColor = ConsoleColor.White;

            lock (Locker)
            {
                _metadata.Information.AppendLine(info);
            }
        }

        protected void CheckAggragateException()
        {
            var text = _metadata.GetExceptionsText();
            if (string.IsNullOrEmpty(text) == false)
            {
                var ex = new PlanarJobAggragateException(text);
                throw ex;
            }
        }

        protected bool CheckIfStopRequest()
        {
            // TODO: to be implement
            return false; // _context.CancellationToken.IsCancellationRequested;
        }

        protected void FailOnStopRequest(Action stopHandle = default)
        {
            // TODO: to be implement
            ////if (stopHandle != default)
            ////{
            ////    stopHandle.Invoke();
            ////}

            ////_context.CancellationToken.ThrowIfCancellationRequested();
        }

        protected T GetData<T>(string key)
        {
            var value = _context.MergeData.GetValueOrDefault(key);
            var result = (T)Convert.ChangeType(value, typeof(T));
            return result;
        }

        protected string GetData(string key)
        {
            return GetData<string>(key);
        }

        protected int? GetEffectedRows()
        {
            return _metadata.EffectedRows;
        }

        protected string GetSetting(string key)
        {
            if (JobSettings.ContainsKey(key))
            {
                return JobSettings[key];
            }
            else
            {
                return null;
            }
        }

        protected T GetSetting<T>(string key)
        {
            if (JobSettings.ContainsKey(key))
            {
                var result = JobSettings[key];

                try
                {
                    return (T)Convert.ChangeType(result, typeof(T));
                }
                catch (Exception ex)
                {
                    throw new ApplicationException($"Fail to convert job settings '{result}' to type {typeof(T).Name}", ex);
                }
            }
            else
            {
                return default;
            }
        }

        protected void IncreaseEffectedRows(int delta = 1)
        {
            lock (Locker)
            {
                _metadata.EffectedRows = _metadata.EffectedRows.GetValueOrDefault() + delta;
            }
        }

        protected DateTime Now()
        {
            if (_isNowOverrideValueExists == null)
            {
                _isNowOverrideValueExists = _context.MergeData.ContainsKey(Consts.NowOverrideValue);
                var value = Convert.ToString(_context.MergeData.GetValueOrDefault(Consts.NowOverrideValue));
                if (DateTime.TryParse(value, out DateTime dateValue))
                {
                    _nowOverrideValue = dateValue;
                }
            }

            if (_nowOverrideValue.HasValue)
            {
                return _nowOverrideValue.Value;
            }
            else
            {
                return DateTime.Now;
            }
        }

        protected void PutJobData(string key, object value)
        {
            _context.MergeData[key] = Convert.ToString(value);
            // _context.JobDetail.JobDataMap.Put(key, value);
        }

        protected void PutTriggerData(string key, object value)
        {
            _context.MergeData[key] = Convert.ToString(value);
            // _context.Trigger.JobDataMap.Put(key, value);
        }

        protected void SetEffectedRows(int value)
        {
            lock (Locker)
            {
                _metadata.EffectedRows = value;
            }
        }

        protected void UpdateProgress(byte value)
        {
            lock (Locker)
            {
                if (value > 100) { value = 100; }
                _metadata.Progress = value;
            }
        }

        protected void UpdateProgress(int current, int total)
        {
            lock (Locker)
            {
                var percentage = 1.0 * current / total;
                if (percentage < 0) { percentage = 0; }
                if (percentage > 1) { percentage = 1; }
                var value = Convert.ToByte(percentage * 100);

                _metadata.Progress = value;
            }
        }
    }
}
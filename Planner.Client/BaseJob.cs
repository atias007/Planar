﻿using Quartz;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planner.Client
{
    public abstract class BaseJob : IJob
    {
        private readonly object Locker = new();
        private IJobExecutionContext _context;
        private bool? _isNowOverrideValueExists;
        private DateTime? _nowOverrideValue;
        private Dictionary<string, string> JobSettings { get; set; } = new();

        public Task Execute(IJobExecutionContext context)
        {
            _context = context;
            return ExecuteJob(context)
                .ContinueWith(t =>
                {
                    _context = null;
                    if (t.Exception != null) { throw t.Exception; }
                });
        }

        public abstract Task ExecuteJob(IJobExecutionContext context);

        public void LoadJobSettings(Dictionary<string, string> settings)
        {
            if (settings != null)
            {
                JobSettings = settings;
            }
        }

        protected void AddAggragateException(Exception ex)
        {
            lock (Locker)
            {
                JobExecutionMetadataUtil.AddAggragateException(_context, ex);
            }
        }

        protected void AppendInformation(string info)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Out.WriteLineAsync(info);
            Console.ForegroundColor = ConsoleColor.White;

            lock (Locker)
            {
                JobExecutionMetadataUtil.AppendInformation(_context, info);
            }
        }

        protected void CheckAggragateException()
        {
            var text = JobExecutionMetadataUtil.GetExceptionsText(_context);
            if (string.IsNullOrEmpty(text) == false)
            {
                var ex = new PlannerJobAggragateException(text);
                throw ex;
            }
        }

        protected bool CheckIfStopRequest()
        {
            return _context.CancellationToken.IsCancellationRequested;
        }

        protected void FailOnStopRequest(Action stopHandle = default)
        {
            if (stopHandle != default)
            {
                stopHandle.Invoke();
            }

            _context.CancellationToken.ThrowIfCancellationRequested();
        }

        protected T GetData<T>(string key)
        {
            var value = _context.MergedJobDataMap.Get(key);
            var result = (T)Convert.ChangeType(value, typeof(T));
            return result;
        }

        protected string GetData(string key)
        {
            return GetData<string>(key);
        }

        protected int? GetEffectedRows()
        {
            return JobExecutionMetadataUtil.GetEffectedRows(_context);
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
                JobExecutionMetadataUtil.IncreaseEffectedRows(_context, delta);
            }
        }

        protected DateTime Now()
        {
            if (_isNowOverrideValueExists == null)
            {
                _isNowOverrideValueExists = _context.MergedJobDataMap.ContainsKey(Consts.NowOverrideValue);
                var value = Convert.ToString(_context.MergedJobDataMap[Consts.NowOverrideValue]);
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
            _context.JobDetail.JobDataMap.Put(key, value);
        }

        protected void PutTriggerData(string key, object value)
        {
            _context.Trigger.JobDataMap.Put(key, value);
        }

        protected void SetEffectedRows(int value)
        {
            lock (Locker)
            {
                JobExecutionMetadataUtil.SetEffectedRows(_context, value);
            }
        }

        protected void UpdateProgress(byte value)
        {
            lock (Locker)
            {
                if (value > 100) { value = 100; }
                JobExecutionMetadataUtil.SetProgress(_context, value);
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

                JobExecutionMetadataUtil.SetProgress(_context, value);
            }
        }
    }
}
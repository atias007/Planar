using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Planar.Job
{
    internal class BaseJobFactory : IBaseJob
    {
        private static readonly object Locker = new object();
        private readonly bool _isNowOverrideValueExists;
        private readonly DateTime? _nowOverrideValue;
        private readonly IJobExecutionContext _context;
        private readonly List<Exception> _exceptions = new List<Exception>();
        private int? _effectedRows;

        public BaseJobFactory(IJobExecutionContext context)
        {
            _context = context;
            _isNowOverrideValueExists = _context.MergedJobDataMap.Exists(Consts.NowOverrideValue);
            if (_isNowOverrideValueExists)
            {
                var stringValue = _context.MergedJobDataMap.Get(Consts.NowOverrideValue);
                if (DateTime.TryParse(stringValue, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out DateTime dateValue))
                {
                    _nowOverrideValue = dateValue;
                }
            }
        }

        #region Context

        public IJobExecutionContext Context => _context;

        #endregion Context

        #region Timing

        public TimeSpan JobRunTime
        {
            get
            {
                return TimeSpan.FromMilliseconds(PlanarJob.Stopwatch.ElapsedMilliseconds);
            }
        }

        public DateTime Now()
        {
            if (_isNowOverrideValueExists && _nowOverrideValue != null)
            {
                return _nowOverrideValue.Value;
            }

            return DateTime.Now;
        }

        #endregion Timing

        #region AggregateException

        public void AddAggregateException(Exception ex)
        {
            lock (Locker)
            {
                var message = new ExceptionDto(ex);
                _exceptions.Add(ex);
                MqttClient.Publish(MessageBrokerChannels.AddAggregateException, message).Wait();
            }
        }

        public void CheckAggragateException()
        {
            lock (Locker)
            {
                if (_exceptions == null || !_exceptions.Any())
                {
                    return;
                }

                if (_exceptions.Count == 1)
                {
                    throw _exceptions[0];
                }

                var seperator = string.Empty.PadLeft(80, '-');
                var sb = new StringBuilder();
                sb.AppendLine($"There is {_exceptions.Count} aggregate exception");
                _exceptions.ForEach(e => sb.AppendLine($"  - {e.Message}"));
                sb.AppendLine(seperator);
                _exceptions.ForEach(e =>
                {
                    sb.AppendLine(e.Message);
                    sb.AppendLine(seperator);
                });

                throw new PlanarJobAggragateException(sb.ToString(), _exceptions);
            }
        }

        public int ExceptionCount => _exceptions.Count;

        #endregion AggregateException

        #region Data

        public void PutJobData(string key, object? value)
        {
            var message = new { Key = key, Value = value };
            MqttClient.Publish(MessageBrokerChannels.PutJobData, message).Wait();
        }

        public void PutTriggerData(string key, object? value)
        {
            var message = new { Key = key, Value = value };
            MqttClient.Publish(MessageBrokerChannels.PutTriggerData, message).Wait();
        }

        #endregion Data

        #region EffectedRows

        public int? EffectedRows
        {
            get { return _effectedRows; }
            set
            {
                lock (Locker)
                {
                    _effectedRows = value;
                    MqttClient.Publish(MessageBrokerChannels.SetEffectedRows, value).Wait();
                }
            }
        }

        #endregion EffectedRows

        #region Inner

        public void ReportException(Exception ex)
        {
            ReportExceptionText(ex.ToString());
        }

        public void ReportExceptionText(string text)
        {
            MqttClient.Publish(MessageBrokerChannels.ReportException, text).Wait();
        }

        #endregion Inner

        #region Progress

        public void UpdateProgress(byte value)
        {
            MqttClient.Publish(MessageBrokerChannels.UpdateProgress, value).Wait();
        }

        public void UpdateProgress(int current, int total)
        {
            var percentage = (1.0 * current / total) * 100;
            if (percentage > byte.MaxValue) { percentage = byte.MaxValue; }
            if (percentage < byte.MinValue) { percentage = byte.MinValue; }
            var value = Convert.ToByte(percentage);
            UpdateProgress(value);
        }

        #endregion Progress
    }
}
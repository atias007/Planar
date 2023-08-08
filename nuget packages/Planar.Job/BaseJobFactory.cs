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
        private bool? _isNowOverrideValueExists;
        private DateTime? _nowOverrideValue;
        private readonly IJobExecutionContext _context;
        private readonly List<ExceptionDto> _exceptions = new List<ExceptionDto>();
        private int? _effectedRows;

        public BaseJobFactory(IJobExecutionContext context)
        {
            _context = context;
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
            if (_isNowOverrideValueExists == null)
            {
                _isNowOverrideValueExists = _context.MergedJobDataMap.Exists(Consts.NowOverrideValue);
                if (_isNowOverrideValueExists.GetValueOrDefault())
                {
                    var value = _context.MergedJobDataMap.Get(Consts.NowOverrideValue);
                    if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out DateTime dateValue))
                    {
                        _nowOverrideValue = dateValue;
                    }
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

        #endregion Timing

        #region AggregateException

        public void AddAggregateException(Exception ex)
        {
            lock (Locker)
            {
                var message = new ExceptionDto(ex);
                _exceptions.Add(message);
                MqttClient.Publish(MessageBrokerChannels.AddAggregateException, message).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        public void CheckAggragateException()
        {
            var text = GetExceptionsText();
            if (!string.IsNullOrWhiteSpace(text))
            {
                var ex = new PlanarJobAggragateException(text);
                throw ex;
            }
        }

        private string GetExceptionsText()
        {
            lock (Locker)
            {
                if (_exceptions == null || !_exceptions.Any())
                {
                    return string.Empty;
                }

                if (_exceptions.Count == 1)
                {
                    return _exceptions[0].ExceptionText ?? string.Empty;
                }

                var seperator = string.Empty.PadLeft(80, '-');
                var sb = new StringBuilder();
                sb.AppendLine($"There is {_exceptions.Count} aggregate exception");
                _exceptions.ForEach(e => sb.AppendLine($"  - {e.Message}"));
                sb.AppendLine(seperator);
                _exceptions.ForEach(e =>
                {
                    sb.AppendLine(e.ExceptionText);
                    sb.AppendLine(seperator);
                });

                return sb.ToString();
            }
        }

        #endregion AggregateException

        #region Data

        public void PutJobData(string key, object? value)
        {
            var message = new { Key = key, Value = value };
            MqttClient.Publish(MessageBrokerChannels.PutJobData, message).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public void PutTriggerData(string key, object? value)
        {
            var message = new { Key = key, Value = value };
            MqttClient.Publish(MessageBrokerChannels.PutTriggerData, message).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        #endregion Data

        #region EffectedRows

        public int? GetEffectedRows()
        {
            return _effectedRows;
        }

        public void SetEffectedRows(int value)
        {
            MqttClient.Publish(MessageBrokerChannels.SetEffectedRows, value).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public void IncreaseEffectedRows(int delta = 1)
        {
            lock (Locker)
            {
                _effectedRows = _effectedRows.GetValueOrDefault() + delta;
                SetEffectedRows(_effectedRows.GetValueOrDefault());
            }
        }

        #endregion EffectedRows

        #region Inner

        public void ReportException(Exception ex)
        {
            MqttClient.Publish(MessageBrokerChannels.ReportException, ex.ToString()).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        #endregion Inner

        #region Progress

        public void UpdateProgress(byte value)
        {
            MqttClient.Publish(MessageBrokerChannels.UpdateProgress, value).ConfigureAwait(false).GetAwaiter().GetResult();
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
using PlanarJobInner;
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

        public string ReportException(Exception ex)
        {
            var text = GetExceptionText(ex);
            var innerEx = GetMostInnerException(ex);
            var dto = new PlanarJobExecutionExceptionDto
            {
                ExceptionText = text,
                Message = ex.Message,
                MostInnerExceptionText = GetExceptionText(innerEx),
                MostInnerMessage = innerEx.Message,
            };

            MqttClient.Publish(MessageBrokerChannels.ReportExceptionV2, dto).Wait();

            return text;
        }

        private static Exception GetMostInnerException(Exception ex)
        {
            var innerException = ex;
            while (innerException.InnerException != null)
            {
                innerException = innerException.InnerException;
            }

            return innerException;
        }

        private static string GetExceptionText(Exception ex)
        {
            var lines = ex.ToString().Split('\n');
            var filterLines = lines
                .Where(l => !l.Contains($"{nameof(Planar)}.{nameof(Job)}\\{nameof(BaseJob)}.cs"))
                .Select(l => l?.TrimEnd());

            var text = string.Join(Environment.NewLine, filterLines);
            return text.Trim();
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
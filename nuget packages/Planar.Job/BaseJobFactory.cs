using PlanarJobInner;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public async Task AddAggregateExceptionAsync(Exception ex, int maxItems = 25)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                var message = new ExceptionDto(ex);
                _exceptions.Add(ex);
                await MqttClient.Publish(MessageBrokerChannels.AddAggregateException, message);

                if (_exceptions.Count >= maxItems)
                {
                    var finalEx = new PlanarJobAggragateException("Aggregate exception items exceeded maximum limit");
                    _exceptions.Add(finalEx);
                    CheckAggragateException();
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public void AddAggregateException(Exception ex, int maxItems = 25)
        {
            AddAggregateExceptionAsync(ex, maxItems).Wait();
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

                ////var seperator = string.Empty.PadLeft(80, '-');
                var sb = new StringBuilder();
                sb.AppendLine($"There is {_exceptions.Count} aggregate exception");
                ////_exceptions.ForEach(e => sb.AppendLine($"  - {e.Message}"));
                ////sb.AppendLine(seperator);
                ////_exceptions.ForEach(e =>
                ////{
                ////    sb.AppendLine(e.Message);
                ////    sb.AppendLine(seperator);
                ////});

                throw new PlanarJobAggragateException(sb.ToString(), _exceptions);
            }
        }

        public int ExceptionCount => _exceptions.Count;

        #endregion AggregateException

        #region Data

        public void PutJobData(string key, object? value)
        {
            PutJobDataAsync(key, value).Wait();
        }

        public async Task PutJobDataAsync(string key, object? value)
        {
            var message = new { Key = key, Value = value };
            await MqttClient.Publish(MessageBrokerChannels.PutJobData, message);
        }

        public void PutTriggerData(string key, object? value)
        {
            PutTriggerDataAsync(key, value).Wait();
        }

        public async Task PutTriggerDataAsync(string key, object? value)
        {
            var message = new { Key = key, Value = value };
            await MqttClient.Publish(MessageBrokerChannels.PutTriggerData, message);
        }

        public void RemoveJobData(string key)
        {
            RemoveJobDataAsync(key).Wait();
        }

        public async Task RemoveJobDataAsync(string key)
        {
            var message = new { Key = key };
            await MqttClient.Publish(MessageBrokerChannels.RemoveJobData, message);
        }

        public void RemoveTriggerData(string key)
        {
            RemoveTriggerDataAsync(key).Wait();
        }

        public async Task RemoveTriggerDataAsync(string key)
        {
            var message = new { Key = key };
            await MqttClient.Publish(MessageBrokerChannels.RemoveTriggerData, message);
        }

        public void ClearJobData()
        {
            ClearJobDataAsync().Wait();
        }

        public async Task ClearJobDataAsync()
        {
            var message = new { };
            await MqttClient.Publish(MessageBrokerChannels.ClearJobData, message);
        }

        public void ClearTriggerData()
        {
            ClearTriggerDataAsync().Wait();
        }

        public async Task ClearTriggerDataAsync()
        {
            var message = new { };
            await MqttClient.Publish(MessageBrokerChannels.ClearTriggerData, message);
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
            var hide = HideStackTrace(ex);
            if (hide) { return ex.Message; }

            const char nl = '\n';
            var lines = ex.ToString().Split(nl);
            var filterLines = lines
                .Where(l => !l.Contains($"{nameof(Planar)}.{nameof(Job)}\\{nameof(BaseJob)}.cs"))
                .Select(l => l?.TrimEnd());

            var text = string.Join(Environment.NewLine, filterLines);
            return text.Trim();
        }

        public static bool HideStackTrace(Exception ex)
        {
            try
            {
                const string propertyName = "HideStackTraceFromPlanar";
                var props = ex.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (props != null)
                {
                    var value = props.GetValue(ex);
                    if (value is bool v)
                    {
                        return v;
                    }
                }

                return false;
            }
            catch
            {
                return false;
                throw;
            }
        }

        #endregion Inner

        #region Progress

        public void UpdateProgress(byte value)
        {
            UpdateProgressAsync(value).Wait();
        }

        public void UpdateProgress(long current, long total)
        {
            UpdateProgressAsync(current, total).Wait();
        }

        public async Task UpdateProgressAsync(byte value)
        {
            await MqttClient.Publish(MessageBrokerChannels.UpdateProgress, value);
        }

        public async Task UpdateProgressAsync(long current, long total)
        {
            var value = CalcProgress(current, total);
            await UpdateProgressAsync(value);
        }

        private static byte CalcProgress(long current, long total)
        {
            var percentage = 1.0 * current / total * 100;
            if (percentage > byte.MaxValue) { percentage = byte.MaxValue; }
            if (percentage < byte.MinValue) { percentage = byte.MinValue; }
            var result = Convert.ToByte(percentage);
            return result;
        }

        #endregion Progress
    }
}
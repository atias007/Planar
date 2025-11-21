using PlanarJobInner;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Job
{
    internal class BaseJobFactory : IBaseJob
    {
        private static readonly object Locker = new object();
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

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

        public async Task AddAggregateExceptionAsync(Exception ex, int maxItems = 25)
        {
            if (ex == null) { return; }

            await _semaphoreSlim.WaitAsync();
            try
            {
                var message = new ExceptionDto(ex);
                _exceptions.Add(ex);
                await MqttClient.PublishAsync(MessageBrokerChannels.AddAggregateException, message);

                if (_exceptions.Count >= maxItems)
                {
                    var topEx = new PlanarJobAggragateException($"Aggregate exception items exceeded maximum limit of {maxItems} exceptions");
                    _exceptions.Insert(0, topEx);
                    CheckAggragateException();
                }
            }
            finally
            {
                _semaphoreSlim.Release();
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

                throw new PlanarJobAggragateException($"There is {_exceptions.Count} aggregate exception", _exceptions);
            }
        }

        public int ExceptionCount => _exceptions.Count;

        #endregion AggregateException

        #region Data

#if NETSTANDARD2_0

        public async Task PutJobDataAsync(string key, object value)
#else
        public async Task PutJobDataAsync(string key, object? value)
#endif
        {
            var message = new { Key = key, Value = value };
            await MqttClient.PublishAsync(MessageBrokerChannels.PutJobData, message);
        }

#if NETSTANDARD2_0

        public async Task PutTriggerDataAsync(string key, object value)
#else
        public async Task PutTriggerDataAsync(string key, object? value)
#endif
        {
            var message = new { Key = key, Value = value };
            await MqttClient.PublishAsync(MessageBrokerChannels.PutTriggerData, message);
        }

        public async Task RemoveJobDataAsync(string key)
        {
            var message = new { Key = key };
            await MqttClient.PublishAsync(MessageBrokerChannels.RemoveJobData, message);
        }

        public async Task RemoveTriggerDataAsync(string key)
        {
            var message = new { Key = key };
            await MqttClient.PublishAsync(MessageBrokerChannels.RemoveTriggerData, message);
        }

        public async Task ClearJobDataAsync()
        {
            var message = new { };
            await MqttClient.PublishAsync(MessageBrokerChannels.ClearJobData, message);
        }

        public async Task ClearTriggerDataAsync()
        {
            var message = new { };
            await MqttClient.PublishAsync(MessageBrokerChannels.ClearTriggerData, message);
        }

        #endregion Data

        #region EffectedRows

        public int? EffectedRows
        {
            get { return _effectedRows; }
        }

        public async Task IncreaseEffectedRowsAsync(int value = 1)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                _effectedRows += value;
                await MqttClient.PublishAsync(MessageBrokerChannels.IncreaseEffectedRows, value);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task SetEffectedRowsAsync(int value)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                _effectedRows = value;
                await MqttClient.PublishAsync(MessageBrokerChannels.SetEffectedRows, value);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        #endregion EffectedRows

        #region Inner

        internal async Task<string> ReportException(Exception ex)
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

            await MqttClient.PublishAsync(MessageBrokerChannels.ReportExceptionV2, dto);

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

        public async Task UpdateProgressAsync(byte value)
        {
            await MqttClient.PublishAsync(MessageBrokerChannels.UpdateProgress, value);
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

        #region Monitor

        public async Task RaiseCustomEventAsync(CustomMonitorEvents customMonitorEvents, string message)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                var number = ((int)customMonitorEvents) - 399;
                var entity = new { Number = number, Message = message };
                await MqttClient.PublishAsync(MessageBrokerChannels.MonitorCustomEvent, entity);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        #endregion Monitor

        #region InvokeJob

#if NETSTANDARD2_0

        public async Task InvokeJobAsync(string id, InvokeJobOptions options = null)
#else
        public async Task InvokeJobAsync(string id, InvokeJobOptions? options = null)
#endif
        {
            ValidateInvokeJobOptions(options);
            await _semaphoreSlim.WaitAsync();
            try
            {
                var entity = new { Id = id, Options = options };
                await MqttClient.PublishAsync(MessageBrokerChannels.InvokeJob, entity);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

#if NETSTANDARD2_0

        public async Task QueueInvokeJobAsync(string id, DateTime dueDate, InvokeJobOptions options = null)
#else
        public async Task QueueInvokeJobAsync(string id, DateTime dueDate, InvokeJobOptions? options = null)
#endif
        {
            ValidateInvokeJobOptions(options);
            await _semaphoreSlim.WaitAsync();
            try
            {
                var entity = new { Id = id, DueDate = dueDate, Options = options };
                await MqttClient.PublishAsync(MessageBrokerChannels.QueueInvokeJob, entity);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

#if NETSTANDARD2_0

        private static void ValidateInvokeJobOptions(InvokeJobOptions options)
#else
        private static void ValidateInvokeJobOptions(InvokeJobOptions? options)
#endif
        {
            if (options == null) { return; }
            if (options.MaxRetries.HasValue && !options.RetrySpan.HasValue)
            {
                throw new PlanarJobException("RetrySpan value must be set when MaxRetries is specified");
            }

            if (options.RetrySpan.HasValue && !options.MaxRetries.HasValue)
            {
                throw new PlanarJobException("MaxRetries value must be set when RetrySpan is specified");
            }
        }

        #endregion InvokeJob
    }
}
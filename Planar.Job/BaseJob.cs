using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Planar.Job
{
    public abstract class BaseJob
    {
        private JobExecutionContext _context;
        private MessageBroker _messageBroker;
        private bool? _isNowOverrideValueExists;
        private DateTime? _nowOverrideValue;

        public Task Execute(string context, ref object messageBroker)
        {
            // TODO: check for deserialize error
            _context = JsonSerializer.Deserialize<JobExecutionContext>(context);
            _messageBroker = new MessageBroker(messageBroker);

            return ExecuteJob(_context)
                .ContinueWith(t =>
                {
                    if (t.Exception != null) { throw t.Exception; }
                });
        }

        public abstract Task ExecuteJob(JobExecutionContext context);

        protected void AddAggragateException(Exception ex)
        {
            var message = new ExceptionDto(ex);
            _messageBroker.Publish(MessageBrokerChannels.AddAggragateException, message);
        }

        protected void AppendInformation(string info)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Out.WriteLineAsync(info);
            Console.ForegroundColor = ConsoleColor.White;

            _messageBroker.Publish(MessageBrokerChannels.AppendInformation, info);
        }

        protected void CheckAggragateException()
        {
            var text = _messageBroker.Publish(MessageBrokerChannels.GetExceptionsText);
            if (string.IsNullOrEmpty(text) == false)
            {
                var ex = new PlanarJobAggragateException(text);
                throw ex;
            }
        }

        protected bool CheckIfStopRequest()
        {
            var text = _messageBroker.Publish(MessageBrokerChannels.CheckIfStopRequest);
            _ = bool.TryParse(text, out var stop);
            return stop;
        }

        protected void FailOnStopRequest(Action stopHandle = default)
        {
            if (stopHandle != default)
            {
                stopHandle.Invoke();
            }

            _messageBroker.Publish(MessageBrokerChannels.FailOnStopRequest);
        }

        protected T GetData<T>(string key)
        {
            var value = _messageBroker.Publish(MessageBrokerChannels.GetData, key);
            var result = (T)Convert.ChangeType(value, typeof(T));
            return result;
        }

        protected string GetData(string key)
        {
            return GetData<string>(key);
        }

        protected bool IsDataExists(string key)
        {
            var text = _messageBroker.Publish(MessageBrokerChannels.DataContainsKey, key);
            _ = bool.TryParse(text, out var result);
            return result;
        }

        protected int? GetEffectedRows()
        {
            var text = _messageBroker.Publish(MessageBrokerChannels.GetEffectedRows);
            _ = int.TryParse(text, out var rows);
            return rows;
        }

        protected string GetSetting(string key)
        {
            if (_context.JobSettings.ContainsKey(key) == false)
            {
                throw new ApplicationException($"Key '{key}' could not found in job settings");
            }

            if (_context.JobSettings.ContainsKey(key))
            {
                return _context.JobSettings[key];
            }
            else
            {
                return null;
            }
        }

        protected T GetSetting<T>(string key)
        {
            if (_context.JobSettings.ContainsKey(key) == false)
            {
                throw new ApplicationException($"Key '{key}' could not found in job settings");
            }

            var result = _context.JobSettings[key];

            try
            {
                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Fail to convert job settings '{result}' to type {typeof(T).Name}", ex);
            }
        }

        protected void IncreaseEffectedRows(int delta = 1)
        {
            _messageBroker.Publish(MessageBrokerChannels.IncreaseEffectedRows, delta);
        }

        protected DateTime Now()
        {
            if (_isNowOverrideValueExists == null)
            {
                _isNowOverrideValueExists = IsDataExists(Consts.NowOverrideValue);
                var value = GetData(Consts.NowOverrideValue);
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
            var message = new { Key = key, Value = value };
            _messageBroker.Publish(MessageBrokerChannels.PutJobData, message);
        }

        protected void PutTriggerData(string key, object value)
        {
            var message = new { Key = key, Value = value };
            _messageBroker.Publish(MessageBrokerChannels.PutTriggerData, message);
        }

        protected void SetEffectedRows(int value)
        {
            _messageBroker.Publish(MessageBrokerChannels.SetEffectedRows, value);
        }

        protected void UpdateProgress(byte value)
        {
            if (value > 100) { value = 100; }
            if (value < 0) { value = 0; }
            _messageBroker.Publish(MessageBrokerChannels.UpdateProgress, value);
        }

        protected void UpdateProgress(int current, int total)
        {
            var percentage = 1.0 * current / total;
            var value = Convert.ToByte(percentage * 100);
            UpdateProgress(value);
        }
    }
}
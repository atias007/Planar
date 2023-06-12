using System;

namespace Planar.Job
{
    internal class BaseJobFactory : IBaseJob
    {
        private readonly MessageBroker _messageBroker;
        private bool? _isNowOverrideValueExists;
        private DateTime? _nowOverrideValue;

        public BaseJobFactory(MessageBroker messageBroker)
        {
            _messageBroker = messageBroker;
        }

        public TimeSpan JobRunTime
        {
            get
            {
                var text = _messageBroker?.Publish(MessageBrokerChannels.JobRunTime);
                var success = double.TryParse(text, out var result);
                if (success)
                {
                    return TimeSpan.FromMilliseconds(result);
                }
                else
                {
                    return TimeSpan.Zero;
                }
            }
        }

        public MessageBroker MessageBroker => _messageBroker;

        public void AddAggregateException(Exception ex)
        {
            var message = new ExceptionDto(ex);
            _messageBroker?.Publish(MessageBrokerChannels.AddAggregateException, message);
        }

        public void CheckAggragateException()
        {
            var text = _messageBroker?.Publish(MessageBrokerChannels.GetExceptionsText);
            if (!string.IsNullOrEmpty(text))
            {
                var ex = new PlanarJobAggragateException(text);
                throw ex;
            }
        }

        [Obsolete("CheckIfStopRequest is no longer supported. Use cancellation token in IJobExecutionContext")]
        public bool CheckIfStopRequest()
        {
            var text = _messageBroker?.Publish(MessageBrokerChannels.CheckIfStopRequest);
            _ = bool.TryParse(text, out var stop);
            return stop;
        }

        [Obsolete("FailOnStopRequest is no longer supported. Use cancellation token in IJobExecutionContext")]
        public void FailOnStopRequest(Action? stopHandle = null)
        {
            if (stopHandle != default)
            {
                stopHandle.Invoke();
            }

            _messageBroker?.Publish(MessageBrokerChannels.FailOnStopRequest);
        }

        public T GetData<T>(string key)
        {
            var value = _messageBroker?.Publish(MessageBrokerChannels.GetData, key);
            var result = (T)Convert.ChangeType(value, typeof(T));
            return result;
        }

        public string GetData(string key)
        {
            return GetData<string>(key);
        }

        public int? GetEffectedRows()
        {
            var text = _messageBroker?.Publish(MessageBrokerChannels.GetEffectedRows);
            _ = int.TryParse(text, out var rows);
            return rows;
        }

        public void IncreaseEffectedRows(int delta = 1)
        {
            _messageBroker?.Publish(MessageBrokerChannels.IncreaseEffectedRows, delta);
        }

        public bool IsDataExists(string key)
        {
            var text = _messageBroker?.Publish(MessageBrokerChannels.DataContainsKey, key);
            _ = bool.TryParse(text, out var result);
            return result;
        }

        public DateTime Now()
        {
            if (_isNowOverrideValueExists == null)
            {
                _isNowOverrideValueExists = IsDataExists(Consts.NowOverrideValue);
                if (_isNowOverrideValueExists.GetValueOrDefault())
                {
                    var value = GetData(Consts.NowOverrideValue);
                    if (DateTime.TryParse(value, out DateTime dateValue))
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

        public void PutJobData(string key, object value)
        {
            var message = new { Key = key, Value = value };
            _messageBroker?.Publish(MessageBrokerChannels.PutJobData, message);
        }

        public void PutTriggerData(string key, object value)
        {
            var message = new { Key = key, Value = value };
            _messageBroker?.Publish(MessageBrokerChannels.PutTriggerData, message);
        }

        public void SetEffectedRows(int value)
        {
            _messageBroker?.Publish(MessageBrokerChannels.SetEffectedRows, value);
        }

        public void UpdateProgress(byte value)
        {
            if (value > 100) { value = 100; }
            if (value < 0) { value = 0; }
            _messageBroker?.Publish(MessageBrokerChannels.UpdateProgress, value);
        }

        public void UpdateProgress(int current, int total)
        {
            var percentage = (1.0 * current / total) * 100;
            if (percentage > byte.MaxValue) { percentage = byte.MaxValue; }
            if (percentage < byte.MinValue) { percentage = byte.MinValue; }
            var value = Convert.ToByte(percentage);
            UpdateProgress(value);
        }
    }
}
using System;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace Planar
{
    internal enum MessageBrokerChannels
    {
        AddAggragateException,
        AppendLog,
        FailOnStopRequest,
        GetExceptionsText,
        GetData,
        CheckIfStopRequest,
        GetEffectedRows,
        IncreaseEffectedRows,
        SetEffectedRows,
        DataContainsKey,
        PutJobData,
        PutTriggerData,
        UpdateProgress,
        JobRunTime
    }

    internal class MessageBroker
    {
        private object Instance { get; set; }
        private readonly object Locker = new();
        private readonly MethodInfo _method;

        public MessageBroker(object instance)
        {
            // TODO: check for null instance

            Instance = instance;
            _method = instance.GetType().GetMethod("Publish");

            if (_method == null)
            {
                throw new ApplicationException("MessageBroker does not contains 'Publish' method");
            }

            Details = GetProperty<string>(instance.GetType(), nameof(Details));
        }

        public string Details { get; set; }

        public string Publish(MessageBrokerChannels channel)
        {
            lock (Locker)
            {
                var result = _method.Invoke(Instance, new object[] { channel.ToString(), null });
                return Convert.ToString(result);
            }
        }

        public string Publish<T>(MessageBrokerChannels channel, T message)
        {
            var messageJson = JsonSerializer.Serialize(message);

            lock (Locker)
            {
                var result = _method.Invoke(Instance, new object[] { channel.ToString(), messageJson });
                return Convert.ToString(result);
            }
        }

        public string Publish(MessageBrokerChannels channel, string message)
        {
            lock (Locker)
            {
                var result = _method.Invoke(Instance, new object[] { channel.ToString(), message });
                return Convert.ToString(result);
            }
        }

        private T GetProperty<T>(Type type, string name)
        {
            var prop = type.GetProperty(name);
            if (prop == null)
            {
                throw new ApplicationException($"MessageBroker does not contains '{name}' property");
            }

            var value = prop.GetValue(Instance);
            var result = (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
            return result;
        }
    }
}
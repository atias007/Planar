using System;
using System.Reflection;
using System.Text.Json;

namespace Planar.Job
{
    public enum MessageBrokerChannels
    {
        AddAggragateException,
        AppendInformation,
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
        UpdateProgress
    }

    internal class MessageBroker
    {
        private object Instance { get; set; }
        private readonly object Locker = new();
        private readonly MethodInfo _method;

        public MessageBroker(object instance)
        {
            Instance = instance;
            _method = instance.GetType().GetMethod("Publish");

            if (_method == null)
            {
                throw new ApplicationException("MessageBroker does not contains 'Publish' method");
            }
        }

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
    }
}
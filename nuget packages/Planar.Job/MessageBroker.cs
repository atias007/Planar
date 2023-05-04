using Planar.Job;
using System;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace Planar
{
    internal class MessageBroker
    {
        private object Instance { get; set; } = new object();
        private readonly object Locker = new object();
        private readonly MethodInfo? _method;

        private MessageBroker()
        {
        }

        public MessageBroker(object instance)
        {
            if (instance == null) { return; }

            Instance = instance;
            _method = instance.GetType().GetMethod("Publish");

            if (_method == null)
            {
                throw new ArgumentNullException(nameof(instance), "MessageBroker does not contains 'Publish' method");
            }

            Details = GetProperty<string>(instance.GetType(), nameof(Details));
        }

        public string Details { get; set; } = string.Empty;

        public string? Publish(MessageBrokerChannels channel)
        {
            lock (Locker)
            {
                var result = _method?.Invoke(Instance, new object?[] { channel.ToString(), null });
                return PlanarConvert.ToString(result);
            }
        }

        public string? Publish<T>(MessageBrokerChannels channel, T message)
        {
            var messageJson = JsonSerializer.Serialize(message);

            lock (Locker)
            {
                var result = _method?.Invoke(Instance, new object[] { channel.ToString(), messageJson });
                return PlanarConvert.ToString(result);
            }
        }

        public string? Publish(MessageBrokerChannels channel, string message)
        {
            lock (Locker)
            {
                var result = _method?.Invoke(Instance, new object[] { channel.ToString(), message });
                return PlanarConvert.ToString(result);
            }
        }

        private T GetProperty<T>(Type type, string name)
        {
            var prop = type.GetProperty(name) ?? throw new PlanarJobException($"MessageBroker does not contains '{name}' property");
            var value = prop.GetValue(Instance);
            var result = (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
            return result;
        }
    }
}
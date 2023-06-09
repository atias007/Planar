using System;
using System.Globalization;
using System.Reflection;

namespace Planar.Monitor.Hook
{
    internal class MessageBroker
    {
        private object Instance { get; set; }
        private readonly object Locker = new object();
        private readonly MethodInfo _method;

        public MessageBroker(object instance)
        {
            // TODO: check for null instance

            Instance = instance;
            var type = Instance.GetType();
            _method = type.GetMethod("LogError");

            if (_method == null)
            {
                throw new PlanarMonitorException("MessageBroker does not contains 'LogError' method");
            }

            Details = GetProperty<string>(type, nameof(Details));
            Users = GetProperty<string>(type, nameof(Users));
            Group = GetProperty<string>(type, nameof(Group));
            GlobalConfig = GetProperty<string>(type, nameof(GlobalConfig));
            Exception = GetProperty<string>(type, nameof(Exception));
            MostInnerException = GetProperty<string>(type, nameof(MostInnerException));
            MostInnerExceptionMessage = GetProperty<string>(type, nameof(MostInnerExceptionMessage));
        }

        public string Details { get; set; }
        public string Users { get; set; }
        public string Group { get; set; }
        public string GlobalConfig { get; set; }
        public string? Exception { get; set; }
        public string? MostInnerException { get; set; }
        public string? MostInnerExceptionMessage { get; set; }

        public string? Publish(Exception? exception, string message, params object?[] args)
        {
            lock (Locker)
            {
                var result = _method.Invoke(Instance, new object?[] { exception, message, args });
                return PlanarConvert.ToString(result);
            }
        }

        private T GetProperty<T>(Type type, string name)
        {
            var prop = type.GetProperty(name) ??
                throw new PlanarMonitorException($"MessageBroker does not contains '{name}' property");

            var value = prop.GetValue(Instance);
            var result = (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
            return result;
        }
    }
}
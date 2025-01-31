using Planar.Common;
using Planar.Hook.Internals;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Planar.Hook
{
    public abstract class BaseHook
    {
        public abstract string Name { get; }

        public abstract string Description { get; }

        public abstract Task Handle(IMonitorDetails monitorDetails);

        public abstract Task HandleSystem(IMonitorSystemDetails monitorDetails);

        internal void Execute(string json)
        {
            var wrapper = JsonSerializer.Deserialize<MonitorMessageWrapper>(json)
                ?? throw new PlanarHookException("Fail to deserialize MonitorMessageWrapper");

            if (wrapper.Subject == nameof(MonitorDetails))
            {
                ExecuteHandle(wrapper);
            }
            else if (wrapper.Subject == nameof(MonitorSystemDetails))
            {
                ExecuteHandleSystem(wrapper);
            }
            else
            {
                throw new PlanarHookException($"MonitorMessageWrapper with subject {wrapper.Subject} are not supported");
            }
        }

        private void ExecuteHandle(MonitorMessageWrapper wrapper)
        {
            try
            {
                var monitorDetails = InitializeMessageDetails<MonitorDetails>(wrapper);
                Handle(monitorDetails).Wait();
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
            }
        }

        private void ExecuteHandleSystem(MonitorMessageWrapper wrapper)
        {
            try
            {
                var monitorDetails = InitializeMessageDetails<MonitorSystemDetails>(wrapper);
                HandleSystem(monitorDetails).Wait();
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
            }
        }

#if NETSTANDARD2_0

        protected static bool IsValidUri(string url)
#else
        protected static bool IsValidUri(string? url)
#endif
        {
            if (string.IsNullOrEmpty(url)) { return false; }
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }

        protected virtual void Log(LogLevel level, string message)
        {
            var key = level.ToString().ToLower();
            var text = Utils.CleanText(message);
            Console.WriteLine($"<hook.log.{key}>{text}</hook.log.{key}>");
        }

        protected virtual void LogError(string message)
        {
            Log(LogLevel.Error, message);
        }

        protected virtual void LogWarning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        protected virtual void LogInformation(string message)
        {
            Log(LogLevel.Information, message);
        }

        protected virtual void LogDebug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        protected virtual void LogCritical(string message)
        {
            Log(LogLevel.Critical, message);
        }

        protected virtual void LogTrace(string message)
        {
            Log(LogLevel.Trace, message);
        }

        private static T InitializeMessageDetails<T>(MonitorMessageWrapper messageBroker)
            where T : Monitor, new()
        {
            var options = new JsonSerializerOptions
            {
                Converters =
                {
                    new TypeMappingConverter<IReadOnlyDictionary<string, string>, Dictionary<string, string>>(),
                }
            };

            var monitorDetails = SafeDeserialize<T>(messageBroker?.Details, options);
            monitorDetails.Users = SafeDeserialize<List<User>>(messageBroker?.Users);
            monitorDetails.Group = SafeDeserialize<Group>(messageBroker?.Group);
#if NETSTANDARD2_0
            monitorDetails.GlobalConfig = SafeDeserialize<Dictionary<string, string>>(messageBroker?.GlobalConfig);
#else
            monitorDetails.GlobalConfig = SafeDeserialize<Dictionary<string, string?>>(messageBroker?.GlobalConfig);
#endif
            return monitorDetails;
        }

#if NETSTANDARD2_0

        private static T SafeDeserialize<T>(string json, JsonSerializerOptions options = null)
#else
        private static T SafeDeserialize<T>(string? json, JsonSerializerOptions? options = null)
#endif
            where T : class, new()
        {
            try
            {
                if (string.IsNullOrEmpty(json))
                {
                    throw new PlanarHookException($"Fail to deserialize {typeof(T).Name}. Json is null or empty");
                }

                var result =
                    options == null ?
                    JsonSerializer.Deserialize<T>(json) :
                    JsonSerializer.Deserialize<T>(json, options);

                return result ?? throw new PlanarHookException($"Fail to deserialize {typeof(T).Name}. Result was null");
            }
            catch (Exception ex)
            {
                throw new PlanarHookException($"Fail to deserialize monitor details context at {nameof(BaseHook)}.{nameof(SafeDeserialize)}", ex);
            }
        }
    }
}
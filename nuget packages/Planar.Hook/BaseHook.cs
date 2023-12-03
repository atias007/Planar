using Planar.Common;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Planar.Hook
{
    public abstract class BaseHook
    {
        public abstract string Name { get; }

        public abstract Task Handle(IMonitorDetails monitorDetails);

        public abstract Task HandleSystem(IMonitorSystemDetails monitorDetails);

        internal Task Execute(string json)
        {
            var wrapper = JsonSerializer.Deserialize<MonitorMessageWrapper>(json)
                ?? throw new PlanarHookException("Fail to deserialize MonitorMessageWrapper");

            if (wrapper.Subject == nameof(MonitorDetails))
            {
                return ExecuteHandle(wrapper);
            }
            else if (wrapper.Subject == nameof(MonitorSystemDetails))
            {
                return ExecuteHandleSystem(wrapper);
            }
            else
            {
                throw new PlanarHookException($"MonitorMessageWrapper with subject {wrapper.Subject} are not supported");
            }
        }

        private Task ExecuteHandle(MonitorMessageWrapper wrapper)
        {
            var monitorDetails = InitializeMessageDetails<MonitorDetails>(wrapper);

            return Handle(monitorDetails)
                .ContinueWith(t =>
                {
                    if (t.Exception != null) { throw t.Exception; }
                });
        }

        private Task ExecuteHandleSystem(MonitorMessageWrapper wrapper)
        {
            var monitorDetails = InitializeMessageDetails<MonitorSystemDetails>(wrapper);

            return HandleSystem(monitorDetails)
                .ContinueWith(t =>
                {
                    if (t.Exception != null) { throw t.Exception; }
                });
        }

        protected static bool IsValidUri(string? url)
        {
            if (string.IsNullOrEmpty(url)) { return false; }
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }

        protected string? GetHookParameter(string key, IMonitor details)
        {
            if (string.IsNullOrEmpty(key))
            {
                LogError("GetHookParameter with key null (or empty) is invalid");
                return null;
            }

            var groupResult = GetHookParameterFromGroup(key, details.Group);
            if (!string.IsNullOrEmpty(groupResult)) { return groupResult; }

            if (details.GlobalConfig.ContainsKey(key))
            {
                return details.GlobalConfig[key];
            }

            LogError($"missing hook parameter with key '{key}' at monitor '{details.MonitorTitle}'");

            return null;
        }

        protected virtual void Log(LogLevel level, string message)
        {
            var key = level.ToString().ToLower();
            Console.WriteLine($"<hook.log.{key}>{message}</hook.log.{key}>");
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

        private static string? GetHookParameterFromGroup(string key, IMonitorGroup group)
        {
            string? url;

            url = GetHookParameterReference(key, group.AdditionalField1);
            if (url != null) { return url; }

            url = GetHookParameterReference(key, group.AdditionalField2);
            if (url != null) { return url; }

            url = GetHookParameterReference(key, group.AdditionalField3);
            if (url != null) { return url; }

            url = GetHookParameterReference(key, group.AdditionalField4);
            if (url != null) { return url; }

            url = GetHookParameterReference(key, group.AdditionalField5);
            if (url != null) { return url; }

            return null;
        }

        private static string? GetHookParameterReference(string key, string? reference)
        {
            if (!string.IsNullOrEmpty(reference) && reference.StartsWith(key))
            {
                var url = reference[key.Length..];
                if (url.StartsWith(':') || url.StartsWith('=') || url.StartsWith(' '))
                {
                    return url[1..];
                }
            }

            return null;
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
            monitorDetails.GlobalConfig = SafeDeserialize<Dictionary<string, string?>>(messageBroker?.GlobalConfig);
            return monitorDetails;
        }

        private static T SafeDeserialize<T>(string? json, JsonSerializerOptions? options = null)
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
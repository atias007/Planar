using Planar.Common;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Planar.Monitor.Hook
{
    public abstract class BaseHook
    {
        private MessageBroker? _messageBroker;

        public abstract string Name { get; }

        public abstract Task Handle(IMonitorDetails monitorDetails);

        public abstract Task HandleSystem(IMonitorSystemDetails monitorDetails);

        internal Task ExecuteHandle(ref object messageBroker)
        {
            InitializeMessageBroker(ref messageBroker);
            var monitorDetails = InitializeMessageDetails<MonitorDetails>(_messageBroker);

            return Handle(monitorDetails)
                .ContinueWith(t =>
                {
                    if (t.Exception != null) { throw t.Exception; }
                });
        }

        internal Task ExecuteHandleSystem(ref object messageBroker)
        {
            InitializeMessageBroker(ref messageBroker);
            var monitorDetails = InitializeMessageDetails<MonitorSystemDetails>(_messageBroker);

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
                LogError(null, "GetHookParameter with key null (or empty) is invalid");
                return null;
            }

            var groupResult = GetHookParameterFromGroup(key, details.Group);
            if (!string.IsNullOrEmpty(groupResult)) { return groupResult; }

            if (details.GlobalConfig.ContainsKey(key))
            {
                return details.GlobalConfig[key];
            }

            LogError(null, $"missing hook parameter with key '{key}' at monitor '{details.MonitorTitle}'");

            return null;
        }

        protected void LogError(Exception? exception, string message, params object?[] args)
        {
            _messageBroker?.Publish(exception, message, args);
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

        private static T InitializeMessageDetails<T>(MessageBroker? messageBroker)
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
                    throw new PlanarMonitorException($"Fail to deserialize {typeof(T).Name}. Json is null or empty");
                }

                var result =
                    options == null ?
                    JsonSerializer.Deserialize<T>(json) :
                    JsonSerializer.Deserialize<T>(json, options);

                return result ?? throw new PlanarMonitorException($"Fail to deserialize {typeof(T).Name}. Result was null");
            }
            catch (Exception ex)
            {
                throw new PlanarMonitorException($"Fail to deserialize monitor details context at {nameof(BaseHook)}.{nameof(SafeDeserialize)}", ex);
            }
        }

        private void InitializeMessageBroker(ref object messageBroker)
        {
            if (messageBroker == null)
            {
                throw new PlanarMonitorException("The MessageBroker provided to hook is null");
            }

            _messageBroker = new MessageBroker(messageBroker);
        }
    }
}
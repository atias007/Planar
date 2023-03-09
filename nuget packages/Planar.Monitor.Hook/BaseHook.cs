using Microsoft.Extensions.Logging;
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

        private void InitializeMessageBroker(ref object messageBroker)
        {
            if (messageBroker == null)
            {
                throw new PlanarMonitorException("The MessageBroker provided to hook is null");
            }

            _messageBroker = new MessageBroker(messageBroker);
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

                if (result == null)
                {
                    throw new PlanarMonitorException($"Fail to deserialize {typeof(T).Name}. Result was null");
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new PlanarMonitorException($"Fail to deserialize monitor details context at {nameof(BaseHook)}.{nameof(ExecuteHandleSystem)}", ex);
            }
        }

        protected void LogError(Exception? exception, string message, params object?[] args)
        {
            _messageBroker?.Publish(exception, message, args);
        }

        protected static bool IsValidUri(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }

        protected string? GetHookParameter(string key, IMonitor details)
        {
            if (string.IsNullOrEmpty(key))
            {
                LogError(null, "GetHookParameter with key null or empty is invalid");
                return null;
            }

            if (details.GlobalConfig.ContainsKey(key))
            {
                return details.GlobalConfig[key];
            }

            return GetHookParameterFromGroup(key, details.Group);
        }

        private static string? GetHookParameterFromGroup(string key, IMonitorGroup group)
        {
            string? url;

            url = GetHookParameterReference(key, group.Reference1);
            if (url != null) { return url; }

            url = GetHookParameterReference(key, group.Reference2);
            if (url != null) { return url; }

            url = GetHookParameterReference(key, group.Reference3);
            if (url != null) { return url; }

            url = GetHookParameterReference(key, group.Reference4);
            if (url != null) { return url; }

            url = GetHookParameterReference(key, group.Reference5);
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

        public abstract Task Handle(IMonitorDetails monitorDetails);

        public abstract Task HandleSystem(IMonitorSystemDetails monitorDetails);

        public abstract string Name { get; }
    }
}
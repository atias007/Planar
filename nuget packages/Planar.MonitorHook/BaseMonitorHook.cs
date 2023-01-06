using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Planar.MonitorHook
{
    public abstract class BaseMonitorHook
    {
        private MessageBroker _messageBroker;

        internal Task ExecuteHandleSystem(ref object messageBroker)
        {
            InitializeMessageBroker(ref messageBroker);
            var monitorDetails = InitializeMessageDetails<MonitorSystemDetails>(_messageBroker.Details);

            return HandleSystem(monitorDetails)
                .ContinueWith(t =>
                {
                    if (t.Exception != null) { throw t.Exception; }
                });
        }

        internal Task ExecuteHandle(ref object messageBroker)
        {
            InitializeMessageBroker(ref messageBroker);
            var monitorDetails = InitializeMessageDetails<MonitorDetails>(_messageBroker.Details);

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

        private T InitializeMessageDetails<T>(string details)
            where T : Monitor
        {
            var options = new JsonSerializerOptions
            {
                Converters =
                {
                    new TypeMappingConverter<IReadOnlyDictionary<string, string>, Dictionary<string, string>>(),
                }
            };

            try
            {
                var monitorDetails = JsonSerializer.Deserialize<T>(_messageBroker.Details, options);
                if (!string.IsNullOrEmpty(_messageBroker.Users))
                {
                    var users = JsonSerializer.Deserialize<List<User>>(_messageBroker.Users);
                    monitorDetails.Users = users;
                }

                if (!string.IsNullOrEmpty(_messageBroker.Group))
                {
                    var group = JsonSerializer.Deserialize<Group>(_messageBroker.Group);
                    monitorDetails.Group = group;
                }

                return monitorDetails;
            }
            catch (Exception ex)
            {
                throw new PlanarMonitorException($"Fail to deserialize monitor details context at {nameof(BaseMonitorHook)}.{nameof(ExecuteHandleSystem)}", ex);
            }
        }

        protected void LogError(Exception exception, string message, params object[] args)
        {
            _messageBroker.Publish(exception, message, args);
        }

        protected static bool IsValidUri(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }

        public abstract Task Handle(IMonitorDetails monitorDetails);

        public abstract Task HandleSystem(IMonitorSystemDetails monitorDetails);

        public abstract Task Test(IMonitorDetails monitorDetails);
    }
}
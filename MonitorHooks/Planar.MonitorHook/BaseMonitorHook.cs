using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.MonitorHook
{
    public abstract class BaseMonitorHook
    {
        private MessageBroker _messageBroker;

        internal Task ExecuteHandle(ref object messageBroker)
        {
            // TODO: check for null instance
            MonitorDetails monitorDetails;
            _messageBroker = new MessageBroker(messageBroker);

            try
            {
                monitorDetails = JsonConvert.DeserializeObject<MonitorDetails>(_messageBroker.Details);
                if (!string.IsNullOrEmpty(_messageBroker.Users))
                {
                    var users = JsonConvert.DeserializeObject<List<User>>(_messageBroker.Users);
                    monitorDetails.Users = users;
                }

                if (!string.IsNullOrEmpty(_messageBroker.Group))
                {
                    var group = JsonConvert.DeserializeObject<Group>(_messageBroker.Group);
                    monitorDetails.Group = group;
                }
            }
            catch (Exception ex)
            {
                throw new PlanarMonitorException("Fail to deserialize monitor details context at BaseMonitorHook.Execute(string)", ex);
            }

            return Handle(monitorDetails)
                .ContinueWith(t =>
                {
                    if (t.Exception != null) { throw t.Exception; }
                });
        }

        protected void LogError(Exception exception, string message, params object[] args)
        {
            _messageBroker.Publish(exception, message, args);
        }

        public abstract Task Handle(IMonitorDetails monitorDetails);

        public abstract Task Test(IMonitorDetails monitorDetails);
    }
}
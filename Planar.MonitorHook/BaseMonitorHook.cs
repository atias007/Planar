using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.MonitorHook
{
    public abstract class BaseMonitorHook
    {
        internal Task ExecuteHandle(string detailsJson, string usersJson, string groupJson)
        {
            MonitorDetails monitorDetails;

            try
            {
                monitorDetails = JsonConvert.DeserializeObject<MonitorDetails>(detailsJson);
                if (string.IsNullOrEmpty(usersJson) == false)
                {
                    var users = JsonConvert.DeserializeObject<List<User>>(usersJson);
                    monitorDetails.Users = users;
                }

                if (string.IsNullOrEmpty(groupJson) == false)
                {
                    var group = JsonConvert.DeserializeObject<Group>(groupJson);
                    monitorDetails.Group = group;
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Fail to deserialize monitor details context at BaseMonitorHook.Execute(string)", ex);
            }

            return Handle(monitorDetails)
                .ContinueWith(t =>
                {
                    if (t.Exception != null) { throw t.Exception; }
                });
        }

        public abstract Task Handle(IMonitorDetails monitorDetails);

        public abstract Task Test(IMonitorDetails monitorDetails);
    }
}
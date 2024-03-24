using System;
using System.Collections.Generic;

namespace Planar.Service.Monitor
{
    public class MonitorSystemInfo(string messageTemplate)
    {
        public string MessageTemplate { get; set; } = messageTemplate;

        public Dictionary<string, string?> MessagesParameters { get; private set; } = [];

        public void AddMachineName()
        {
            MessagesParameters.Add("MachineName", Environment.MachineName);
        }
    }
}
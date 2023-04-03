using System;
using System.Collections.Generic;

namespace Planar.Service.Monitor
{
    public class MonitorSystemInfo
    {
        public MonitorSystemInfo(string messageTemplate)
        {
            MessageTemplate = messageTemplate;
        }

        public string MessageTemplate { get; set; }

        public Dictionary<string, string?> MessagesParameters { get; private set; } = new();

        public void AddMachineName()
        {
            MessagesParameters.Add("MachineName", Environment.MachineName);
        }
    }
}
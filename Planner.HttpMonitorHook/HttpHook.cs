using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planner.MonitorHook;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Planner.HttpMonitorHook
{
    public class HttpHook : IMonitorHook
    {
        public Task Handle(IMonitorDetails monitorDetails, ILogger logger)
        {
            var json = JsonConvert.SerializeObject(monitorDetails);
            return File.WriteAllTextAsync($@"C:\temp\{DateTime.Now.Ticks}.txt", json);
        }
    }
}
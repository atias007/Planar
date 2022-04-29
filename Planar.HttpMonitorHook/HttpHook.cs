using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar.MonitorHook;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Planar.HttpMonitorHook
{
    public class HttpHook : IMonitorHook
    {
        public Task Handle(IMonitorDetails monitorDetails, ILogger logger)
        {
            var json = JsonConvert.SerializeObject(monitorDetails);
            return File.WriteAllTextAsync($@"C:\temp\{DateTime.Now.Ticks}.txt", json);
        }

        public Task Test(IMonitorDetails monitorDetails, ILogger logger)
        {
            throw new NotImplementedException();
        }
    }
}
using Planar.MonitorHook;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Planar.HttpMonitorHook
{
    public class HttpHook : BaseMonitorHook
    {
        public override Task Handle(IMonitorDetails monitorDetails)
        {
            var json = JsonSerializer.Serialize(monitorDetails);
            return File.WriteAllTextAsync($@"C:\temp\{DateTime.Now.Ticks}.txt", json);
        }

        public override Task Test(IMonitorDetails monitorDetails)
        {
            throw new NotImplementedException();
        }
    }
}
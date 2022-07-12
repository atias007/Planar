using Planar.MonitorHook;
using System;
using System.Threading.Tasks;

namespace Planar.HttpMonitorHook
{
    public class HttpHook : BaseMonitorHook
    {
        public override Task Handle(IMonitorDetails monitorDetails)
        {
            throw new NotImplementedException();
        }

        public override Task Test(IMonitorDetails monitorDetails)
        {
            throw new NotImplementedException();
        }
    }
}
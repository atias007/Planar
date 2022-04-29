using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Planar.MonitorHook
{
    public interface IMonitorHook
    {
        Task Handle(IMonitorDetails monitorDetails, ILogger logger);

        Task Test(IMonitorDetails monitorDetails, ILogger logger);
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Common.Monitor
{
    public interface IMonitorDurationDataLayer
    {
        Task<List<MonitorCacheItem>> GetDurationMonitorActions();
    }
}
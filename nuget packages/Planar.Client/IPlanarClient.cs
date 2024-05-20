using Planar.Client.Api;
using Planar.Client.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client
{
    public interface IPlanarClient
    {
        IClusterApi Cluster { get; }
        IConfigApi Config { get; }
        IGroupApi Group { get; }
        IHistoryApi History { get; }
        IJobApi Job { get; }
        IMetricsApi Metrics { get; }
        IMonitorApi Monitor { get; }
        IReportApi Report { get; }
        IServiceApi Service { get; }
        ITriggerApi Trigger { get; }
        IUserApi User { get; }
    }
}
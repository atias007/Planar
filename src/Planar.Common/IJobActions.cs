using Planar.API.Common.Entities;
using System.Threading.Tasks;

namespace Planar.Common;

public interface IJobActions
{
    Task<PlanarIdResponse> QueueInvoke(QueueInvokeJobRequest request);
}
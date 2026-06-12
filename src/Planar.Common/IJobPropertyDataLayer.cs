using System.Threading.Tasks;

namespace Planar.Common
{
    public interface IJobPropertyDataLayer
    {
        Task<(string? Properties, string? GlobalConfigKeys)> GetJobProperty(string jobId);
    }
}
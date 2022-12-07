using System.Threading.Tasks;

namespace Planar.Common
{
    public interface IJobPropertyDataLayer
    {
        Task<string> GetJobProperty(string jobId);
    }
}
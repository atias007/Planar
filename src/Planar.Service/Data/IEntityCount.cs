using System.Threading.Tasks;

namespace Planar.Service.Data;

public interface IEntityCount
{
    Task<int> Count();
}
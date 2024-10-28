using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.Data;

public interface IAutoMapperData : IBaseDataLayer
{
    Task<int> GetGroupId(string name);

    Task<string?> GetGroupName(int id);
}

public class AutoMapperDataSqlite(PlanarContext context) : AutoMapperData(context), IAutoMapperData
{
}

public class AutoMapperDataSqlServer(PlanarContext context) : AutoMapperData(context), IAutoMapperData
{
}

public class AutoMapperData(PlanarContext context) : BaseDataLayer(context)
{
    public async Task<int> GetGroupId(string name)
    {
        return await _context.Groups
            .AsNoTracking()
            .Where(g => g.Name == name)
            .Select(g => g.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<string?> GetGroupName(int id)
    {
        return await _context.Groups
            .AsNoTracking()
            .Where(g => g.Id == id)
            .Select(g => g.Name)
            .FirstOrDefaultAsync();
    }
}
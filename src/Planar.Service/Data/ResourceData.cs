using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Service.Model;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.Data;

public interface IResourceData : IBaseDataLayer
{
    Task AddResource(Resource resource);

    Task<int> RemoveResource(string name);

    Task<Resource?> GetResource(string name);

    IQueryable<Resource> GetResources(PagingRequest request);

    Task<int> UpdateResource(Resource resource);
}

public class ResourceDataSqlite(PlanarContext context) : ResourceData(context), IResourceData
{
}

public class ResourceDataSqlServer(PlanarContext context) : ResourceData(context), IResourceData
{
}

public class ResourceData(PlanarContext context) : BaseDataLayer(context), IResourceData
{
    public async Task AddResource(Resource resource)
    {
        _context.Resources.Add(resource);
        await _context.SaveChangesAsync();
    }

    public async Task<Resource?> GetResource(string name)
    {
        return await _context.Resources.AsNoTracking().FirstOrDefaultAsync(r => r.Name == name);
    }

    public IQueryable<Resource> GetResources(PagingRequest request)
    {
        return _context.Resources.AsNoTracking().Select(r => new Resource
        {
            Name = r.Name,
            Value = r.Value.Substring(0, 101)
        });
    }

    public async Task<int> RemoveResource(string name)
    {
        return await _context.Resources.Where(r => r.Name == name).ExecuteDeleteAsync();
    }

    public async Task<int> UpdateResource(Resource resource)
    {
        return await _context.Resources.Where(r => r.Name == resource.Name)
             .ExecuteUpdateAsync(r => r.SetProperty(x => x.Value, resource.Value));
    }
}
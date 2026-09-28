using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Data;

public interface IResourceData : IBaseDataLayer, IJobResourceDataLayer
{
    Task AddResource(Resource resource);

    Task<int> RemoveResource(string name);

    Task<Resource?> GetResource(string name);

    IQueryable<Resource> GetResources(PagingRequest request);

    Task<int> UpdateResource(Resource resource);

    Task<IEnumerable<string>> GetAllResourceNames();

    Task<bool> Exists(string name);
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

    public async Task<IEnumerable<string>> GetAllResourceNames()
    {
        return await _context.Resources
            .AsNoTracking()
            .Select(r => r.Name)
            .OrderBy(r => r)
            .ToListAsync();
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

    public async Task<bool> Exists(string name)
    {
        return await _context.Resources.AsNoTracking().AnyAsync(r => r.Name == name);
    }

    public async Task<string?> GetResourceValue(string name)
    {
        return await _context.Resources.AsNoTracking()
            .Where(r => r.Name == name)
            .Select(r => r.Value)
            .FirstOrDefaultAsync();
    }

    public async Task<Dictionary<string, string>> GetResources(IEnumerable<string> names, CancellationToken cancellationToken)
    {
        var resources = await _context.Resources.AsNoTracking()
            .Where(r => names.Contains(r.Name))
            .Select(r => new KeyValuePair<string, string>(r.Name, r.Value))
            .ToListAsync(cancellationToken);

        return resources.ToDictionary(k => k.Key, v => v.Value);
    }
}
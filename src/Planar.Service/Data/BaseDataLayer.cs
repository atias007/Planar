using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Threading.Tasks;

namespace Planar.Service.Data;

public interface IBaseDataLayer
{
    IDbConnection DbConnection { get; }

    Task<int> SaveChangesAsync();

    Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess);
}

public abstract class BaseDataLayer(PlanarContext context) : IBaseDataLayer
{
    protected readonly PlanarContext _context = context ?? throw new PlanarJobException(nameof(context));

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess)
    {
        return await _context.SaveChangesAsync(acceptAllChangesOnSuccess);
    }

    public IDbConnection DbConnection => _context.Database.GetDbConnection();
}

public abstract class BaseTraceDataLayer(PlanarTraceContext context) : IBaseDataLayer
{
    protected readonly PlanarTraceContext _context = context ?? throw new PlanarJobException(nameof(context));

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess)
    {
        return await _context.SaveChangesAsync(acceptAllChangesOnSuccess);
    }

    public IDbConnection DbConnection => _context.Database.GetDbConnection();
}
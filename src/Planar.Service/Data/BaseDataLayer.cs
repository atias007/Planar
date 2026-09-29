using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Planar.Service.Data;

public interface IBaseDataLayer
{
    DbConnection DbConnection { get; }

    Task<int> SaveChangesAsync();

    Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess);

    bool IsDuplicateKey(DbUpdateException ex);
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

    public DbConnection DbConnection => _context.Database.GetDbConnection();

    // 2627 = PK / unique constraint violation, 2601 = unique index violation
    public bool IsDuplicateKey(DbUpdateException ex) => DbFactory.IsDuplicateKey(ex);
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

    public DbConnection DbConnection => _context.Database.GetDbConnection();

    public bool IsDuplicateKey(DbUpdateException ex) => DbFactory.IsDuplicateKey(ex);
}
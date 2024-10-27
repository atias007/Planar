using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Threading.Tasks;

namespace Planar.Service.Data;

public abstract class BaseDataLayer
{
    protected readonly PlanarContext _context;

    protected BaseDataLayer(PlanarContext context)
    {
        _context = context ?? throw new PlanarJobException(nameof(context));
    }

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
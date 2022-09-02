using Microsoft.EntityFrameworkCore;

namespace EFCoreTest
{
    internal class PlanarDbContext : DbContext
    {
        public PlanarDbContext(DbContextOptions<PlanarDbContext> options)
           : base(options)
        {
        }

        public DbSet<JobInstanceLog> JobInstanceLogs { get; set; }
    }
}
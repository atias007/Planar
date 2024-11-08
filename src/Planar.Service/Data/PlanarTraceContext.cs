using Microsoft.EntityFrameworkCore;
using Planar.Service.Model;

namespace Planar.Service.Data;

public class PlanarTraceContext(DbContextOptions<PlanarTraceContext> options) : DbContext(options)
{
    public virtual DbSet<Trace2> Traces { get; set; }
}
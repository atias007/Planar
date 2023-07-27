using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Service.Model;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.Data
{
    public class ServiceData : BaseDataLayer
    {
        public ServiceData(PlanarContext context) : base(context)
        {
        }

        public async Task HealthCheck()
        {
            const string query = "SELECT 1";
            await _context.Database.ExecuteSqlRawAsync(query);
        }

        public async Task AddSecurityAudit(SecurityAudit audit)
        {
            _context.Add(audit);
            await _context.SaveChangesAsync();
        }

        public IQueryable<SecurityAudit> GetSecurityAudits(SecurityAuditsFilter request)
        {
            var query = _context.SecurityAudits.AsNoTracking();

            if (request.FromDate.HasValue)
            {
                query = query.Where(l => l.DateCreated >= request.FromDate);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(l => l.DateCreated < request.ToDate);
            }

            if (request.Ascending)
            {
                query = query.OrderBy(l => l.DateCreated);
            }
            else
            {
                query = query.OrderByDescending(l => l.DateCreated);
            }

            return query;
        }
    }
}
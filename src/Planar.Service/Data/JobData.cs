using Microsoft.EntityFrameworkCore;
using Planar.Common;
using Planar.Service.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.Data
{
    public class JobData : BaseDataLayer, IJobPropertyDataLayer
    {
        public JobData(PlanarContext context) : base(context)
        {
        }

        public async Task<string?> GetJobProperty(string jobId)
        {
            var properties = await _context.JobProperties
                .Where(j => j.JobId == jobId)
                .Select(j => j.Properties)
                .FirstOrDefaultAsync();

            return properties;
        }

        public async Task<IEnumerable<string>> GetJobPropertiesIds()
        {
            var properties = await _context.JobProperties
                .AsNoTracking()
                .Select(j => j.JobId)
                .ToListAsync();

            return properties;
        }

        public async Task DeleteJobProperty(string jobId)
        {
            await _context.JobProperties.Where(p => p.JobId == jobId).ExecuteDeleteAsync();
        }

        public async Task AddJobProperty(JobProperty jobProperty)
        {
            _context.JobProperties.Add(jobProperty);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateJobProperty(JobProperty jobProperty)
        {
            _context.JobProperties.Update(jobProperty);
            await _context.SaveChangesAsync();
        }
    }
}
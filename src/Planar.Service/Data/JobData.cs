using Microsoft.EntityFrameworkCore;
using Planar.Common;
using Planar.Service.Model;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.Data
{
    public class JobData : BaseDataLayer, IJobPropertyDataLayer
    {
        public JobData(PlanarContext context) : base(context)
        {
        }

        public async Task<string> GetJobProperty(string jobId)
        {
            var properties = await _context.JobProperties
                .Where(j => j.JobId == jobId)
                .Select(j => j.Properties)
                .FirstOrDefaultAsync();

            return properties;
        }

        public async Task DeleteJobProperty(string jobId)
        {
            var p = new JobProperty { JobId = jobId };
            _context.JobProperties.Remove(p);
            await SaveChangesWithoutConcurrency();
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
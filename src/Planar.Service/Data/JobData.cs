using Microsoft.EntityFrameworkCore;
using Planar.Common;
using Planar.Service.Model;
using Planar.Service.Reports;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Planar.Service.Data;

public class JobData(PlanarContext context) : BaseDataLayer(context), IJobPropertyDataLayer
{
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

    public async Task AddJobAudit(JobAudit jobAudit)
    {
        _context.JobAudits.Add(jobAudit);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteJobAudit(string jobId)
    {
        await _context.JobAudits.Where(j => j.JobId == jobId).ExecuteDeleteAsync();
    }

    public IQueryable<JobAudit> GetJobAudits(string id)
    {
        return _context.JobAudits
            .AsNoTracking()
            .Where(a => a.JobId == id || a.JobId == string.Empty)
            .OrderByDescending(a => a.DateCreated)
            .ThenByDescending(a => a.Id);
    }

    public IQueryable<JobAudit> GetAudits()
    {
        return _context.JobAudits
            .AsNoTracking()
            .OrderByDescending(a => a.DateCreated)
            .ThenByDescending(a => a.Id);
    }

    public IQueryable<JobAudit> GetAuditsForReport(DateScope dateScope)
    {
        return _context.JobAudits
            .AsNoTracking()
            .Where(a => a.DateCreated >= dateScope.From && a.DateCreated < dateScope.To)
            .Take(1000)
            .OrderByDescending(a => a.DateCreated)
            .ThenByDescending(a => a.Id);
    }

    public IQueryable<JobAudit> GetJobAudit(int id)
    {
        return _context.JobAudits
            .AsNoTracking()
            .Where(a => a.Id == id);
    }
}
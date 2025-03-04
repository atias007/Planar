﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Planar.Common;
using Planar.Service.Model;
using Planar.Service.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.Data;

public interface IJobData : IJobPropertyDataLayer, IBaseDataLayer
{
    Task AddJobAudit(JobAudit jobAudit);

    Task AddJobProperty(JobProperty jobProperty);

    Task<IEnumerable<string>> GetUnknownJobProperties();

    Task UpdatePropertiesJobType(string id, string type);

    Task DeleteJobAudit(string jobId);

    Task DeleteJobProperty(string jobId);

    IQueryable<JobAudit> GetAudits();

    IQueryable<JobAudit> GetAuditsForReport(DateScope dateScope);

    IQueryable<JobAudit> GetJobAudit(int id);

    IQueryable<JobAudit> GetJobAudits(string id, int firstId);

    Task<int?> GetJobFirstAudit(string id);

    Task<IEnumerable<string>> GetJobPropertiesIds();

    Task UpdateJobProperty(JobProperty jobProperty);

    Task<IEnumerable<JobProperty>> GetAllProperties(string typeName);
}

public class JobDataSqlite(PlanarContext context) : JobData(context), IJobData
{
}

public class JobDataSqlServer(PlanarContext context) : JobData(context), IJobData
{
}

public class JobData(PlanarContext context) : BaseDataLayer(context)
{
    public async Task<IEnumerable<JobProperty>> GetAllProperties(string typeName)
    {
        var properties = await _context.JobProperties
            .Where(j => j.JobType == typeName)
            .ToListAsync();

        return properties;
    }

    public async Task<string?> GetJobProperty(string jobId)
    {
        var properties = await _context.JobProperties
            .Where(j => j.JobId == jobId)
            .Select(j => j.Properties)
            .FirstOrDefaultAsync();

        return properties;
    }

    public async Task<IEnumerable<string>> GetUnknownJobProperties()
    {
        var ids = await _context.JobProperties
           .Where(j => j.JobType == "Unknown" || j.JobType == string.Empty)
           .Select(j => j.JobId)
           .ToListAsync();

        return ids;
    }

    public async Task UpdatePropertiesJobType(string id, string jobType)
    {
        await _context.JobProperties
            .Where(j => j.JobId == id)
            .ExecuteUpdateAsync(u => u.SetProperty(p => p.JobType, jobType));
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

    public async Task<int?> GetJobFirstAudit(string id)
    {
        var result = await _context.JobAudits
            .AsNoTracking()
            .Where(a => a.JobId == id)
            .Select(a => a.Id)
            .OrderBy(a => a)
            .FirstOrDefaultAsync();

        return result;
    }

    public IQueryable<JobAudit> GetJobAudits(string id, int firstId)
    {
        return _context.JobAudits
            .AsNoTracking()
            .Where(a => a.JobId == id || (a.JobId == string.Empty && a.Id >= firstId))
            .Where(a => !a.JobKey.StartsWith(Consts.PlanarSystemGroup))
            .OrderByDescending(a => a.DateCreated)
            .ThenByDescending(a => a.Id);
    }

    public IQueryable<JobAudit> GetAudits()
    {
        return _context.JobAudits
            .AsNoTracking()
            .Where(a => !a.JobKey.StartsWith(Consts.PlanarSystemGroup))
            .OrderByDescending(a => a.DateCreated)
            .ThenByDescending(a => a.Id);
    }

    public IQueryable<JobAudit> GetAuditsForReport(DateScope dateScope)
    {
        return _context.JobAudits
            .AsNoTracking()
            .Where(a => a.DateCreated >= dateScope.From && a.DateCreated < dateScope.To)
            .Where(a => !a.JobKey.StartsWith(Consts.PlanarSystemGroup))
            .Take(1000)
            .OrderByDescending(a => a.DateCreated)
            .ThenByDescending(a => a.Id);
    }

    public IQueryable<JobAudit> GetJobAudit(int id)
    {
        return _context.JobAudits
            .AsNoTracking()
            .Where(a => !a.JobKey.StartsWith(Consts.PlanarSystemGroup))
            .Where(a => a.Id == id);
    }
}
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Common.Helpers;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.Model;
using Planar.Service.Monitor;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.API;

public partial class JobDomain
{
    public async Task<PlanarIdResponse> UpdateRoute(HttpContext httpContext)
    {
        var contentType = httpContext.Request.ContentType ?? string.Empty;
        if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            var entity = await httpContext.Request.ReadFromJsonAsync<UpdateJobRequest>();
            ArgumentNullException.ThrowIfNull(entity);
            var validator = Resolve<IValidator<UpdateJobRequest>>();
            await validator.ValidateAndThrowAsync(entity);
            return await Update(entity);
        }
        else if (contentType.Contains("yaml", StringComparison.OrdinalIgnoreCase))
        {
            using var reader = new StreamReader(httpContext.Request.Body);
            var yml = await reader.ReadToEndAsync();
            return await Update(yml);
        }

        throw new RestValidationException("contentType", $"Unsupported content type: {contentType}");
    }

    private static T? Clone<T>(T obj)
        where T : class, new()
    {
        var json = YmlUtil.Serialize(obj);
        var result = YmlUtil.Deserialize<T>(json);
        return result;
    }

    private static IEnumerable<BaseTrigger> GetAllTriggers(SetJobRequest request)
    {
        var allTriggers = new List<BaseTrigger>();
        if (request.SimpleTriggers != null)
        {
            allTriggers.AddRange(request.SimpleTriggers);
        }

        if (request.CronTriggers != null)
        {
            allTriggers.AddRange(request.CronTriggers);
        }

        return allTriggers;
    }

    private static void SyncJobData(SetJobRequest request, JobUpdateMetadata metadata)
    {
        foreach (var item in metadata.OldJobDetails.JobDataMap)
        {
            request.JobData.Put<string?>(item.Key, PlanarConvert.ToString(item.Value));
        }
    }

    private static void SyncTriggersData(SetJobRequest request, JobUpdateMetadata metadata)
    {
        var allTriggers = GetAllTriggers(request);
        foreach (var oldTrigger in metadata.OldTriggers)
        {
            var updateTrigger = allTriggers.FirstOrDefault(t => t.Group == oldTrigger.Key.Group && t.Name == oldTrigger.Key.Name);
            if (updateTrigger == null) { continue; }
            foreach (var data in oldTrigger.JobDataMap)
            {
                updateTrigger.TriggerData.Put<string?>(data.Key, PlanarConvert.ToString(data.Value));
            }

            var dataCount = CountUserJobDataItems(updateTrigger.TriggerData);
            if (dataCount > Consts.MaximumJobDataItems)
            {
                throw new RestValidationException("trigger data", $"trigger data items exceeded maximum limit of {Consts.MaximumJobDataItems}");
            }
        }
    }

    private static void UpdateJobData(SetJobDynamicRequest request, UpdateJobOptions options, JobUpdateMetadata metadata)
    {
        if (options.UpdateJobData)
        {
            // Preserve the same id
            request.JobData.Add(Consts.JobId, metadata.JobId);
        }
        else
        {
            // Sync the old data including the id
            request.JobData.Clear();
            SyncJobData(request, metadata);
        }

        BuildJobData(request, metadata.JobDetails);

        var dataCount = CountUserJobDataItems(request.JobData);
        if (dataCount > Consts.MaximumJobDataItems)
        {
            throw new RestValidationException("job data", $"job data items exceeded maximum limit of {Consts.MaximumJobDataItems}");
        }
    }

    private static void UpdateJobExtendedProperties(SetJobDynamicRequest request, JobUpdateMetadata metadata)
    {
        // Add: Author, CircuitBreaker, RetentionDays
        AddAuthor(request, metadata.JobDetails);
        AddCircuitBreaker(request, metadata.JobDetails);
        AddLogRetentionDays(request, metadata.JobDetails);
    }

    private static void ValidateUpdateJobOptions(UpdateJobOptions options)
    {
        if (options == null)
        {
            throw new RestValidationException("request", "options property is null or empty");
        }
    }

    private async Task FillRollbackData(JobUpdateMetadata metadata)
    {
        var scheduler = await GetScheduler();
        metadata.OldJobDetails =
            await scheduler.GetJobDetail(metadata.JobKey)
            ?? throw new RestGeneralException($"job with key '{KeyHelper.GetKeyTitle(metadata.JobKey)}' could not be found");

        metadata.OldTriggers = await scheduler.GetTriggersOfJob(metadata.JobKey);
        await scheduler.DeleteJob(metadata.JobKey);
        metadata.EnableRollback();
        metadata.OldJobProperties = await DataLayer.GetJobProperty(metadata.JobId);
        metadata.Author = JobHelper.GetJobAuthor(metadata.OldJobDetails);
        metadata.LogRetentionDays = JobHelper.GetLogRetentionDays(metadata.OldJobDetails);
    }

    private async Task RollBack(JobUpdateMetadata metadata)
    {
        if (metadata == null) { return; }
        if (metadata.OldJobDetails == null) { return; }
        if (!metadata.RollbackEnabled) { return; }

        var jobType = General.SchedulerUtil.GetJobTypeName(metadata.OldJobDetails);
        var property = new JobProperty { JobId = metadata.JobId, Properties = metadata.OldJobProperties, JobType = jobType };
        var scheduler = await GetScheduler();
        await scheduler.ScheduleJob(metadata.OldJobDetails, metadata.OldTriggers, true);
        await scheduler.PauseJob(metadata.JobKey);
        await Resolve<IJobData>().UpdateJobProperty(property);
    }

    private async Task<PlanarIdResponse> Update(string yml)
    {
        var dynamicRequest = await GetDynamicRequest(yml);
        return await Update(dynamicRequest, UpdateJobOptions.Default);
    }

    private async Task<PlanarIdResponse> Update(SetJobDynamicRequest request, UpdateJobOptions options)
    {
        var metadata = new JobUpdateMetadata();

        try
        {
            return await UpdateInner(request, options, metadata);
        }
        catch
        {
            await RollBack(metadata);
            throw;
        }
    }

    private async Task<PlanarIdResponse> Update(UpdateJobRequest request)
    {
        var dynamicRequest = await GetDynamicRequest(request);
        SetDynamicRequestPath(dynamicRequest, request.JobFilePath);
        var response = await Update(dynamicRequest, request.Options);
        return response;
    }

    private async Task<PlanarIdResponse> UpdateInner(SetJobDynamicRequest request, UpdateJobOptions options, JobUpdateMetadata metadata)
    {
        // Validation
        await ValidateUpdateJob(request, options, metadata);

        // save paused triggers before pause job
        metadata.PausedTriggers = await GetPausedTriggers(metadata.JobKey);

        // pause the job to avoid trigger firing during update
        var scheduler = await GetScheduler();
        await scheduler.PauseJob(metadata.JobKey);

        // Lock monitor events
        MonitorUtil.Lock(metadata.JobKey, lockSeconds: 5, MonitorEvents.JobDeleted, MonitorEvents.JobAdded, MonitorEvents.JobPaused);

        // Save for rollback
        await FillRollbackData(metadata);

        // Clone request for audit
        var cloneRequest = Clone(request);

        // Update Job Details (JobType+Concurrent, JobGroup, JobName, Description, Durable)
        await UpdateJobDetails(request, metadata);

        // Sync Job Data
        UpdateJobData(request, options, metadata);

        // Update the author
        UpdateJobExtendedProperties(request, metadata);

        // Update Triggers
        await UpdateTriggers(request, metadata);

        // ScheduleJob
        try
        {
            await scheduler.ScheduleJob(metadata.JobDetails, metadata.Triggers, true);
        }
        catch (Exception ex)
        {
            ValidateTriggerNeverFire(ex);
            throw;
        }

        await PauseTriggers(metadata.JobKey, metadata.PausedTriggers);

        // Update Properties
        await UpdateJobProperties(request, metadata);

        // Audit
        AuditJobSafe(metadata.JobKey, "job updated", new { request = cloneRequest, options });
        SafeRefreshJobDetailsCache();

        // Monitoring
        var info = new MonitorSystemInfo("Job {{JobGroup}}.{{JobName}} (Id: {{JobId}}) was updated");
        info.MessagesParameters.Add("JobGroup", metadata.JobKey.Group);
        info.MessagesParameters.Add("JobName", metadata.JobKey.Name);
        info.MessagesParameters.Add("JobId", metadata.JobId);
        info.AddMachineName();
        MonitorUtil.SafeSystemScan(ServiceProvider, Logger, MonitorEvents.ClusterNodeJoin, info);

        // Return Id
        return new PlanarIdResponse { Id = metadata.JobId };
    }

    private async Task UpdateJobDetails(SetJobDynamicRequest request, JobUpdateMetadata metadata)
    {
        var scheduler = await GetScheduler();
        await scheduler.DeleteJob(metadata.JobKey);
        metadata.JobDetails = BuildJobDetails(request, metadata.JobKey);
    }

    private async Task UpdateJobProperties(SetJobDynamicRequest request, JobUpdateMetadata metadata)
    {
        var jobPropertiesYml = GetJopPropertiesYml(request);
        var jobType = General.SchedulerUtil.GetJobTypeName(metadata.JobDetails);
        var property = new JobProperty { JobId = metadata.JobId, Properties = jobPropertiesYml, JobType = jobType };

        if (string.IsNullOrEmpty(metadata.OldJobProperties))
        {
            await DataLayer.AddJobProperty(property);
        }
        else
        {
            await DataLayer.UpdateJobProperty(property);
        }
    }

    private async Task UpdateTriggers(SetJobRequest request, JobUpdateMetadata metadata)
    {
        var scheduler = await GetScheduler();
        foreach (var item in metadata.OldTriggers)
        {
            await scheduler.UnscheduleJob(item.Key);
        }

        SyncTriggersData(request, metadata);
        metadata.Triggers = BuildTriggers(request, metadata.JobId);
    }

    private async Task ValidateUpdateJob(SetJobDynamicRequest request, UpdateJobOptions options, JobUpdateMetadata metadata)
    {
        ValidateRequestNoNull(request);
        ValidateUpdateJobOptions(options);
        await ValidateRequestProperties(request);
        var scheduler = await GetScheduler();
        metadata.JobKey = ValidateJobMetadata(request, scheduler);
        ValidateSystemJob(metadata.JobKey);
        await JobKeyHelper.ValidateJobExists(metadata.JobKey);
        metadata.JobId =
            await JobKeyHelper.GetJobId(metadata.JobKey) ??
            throw new RestGeneralException($"could not find job id for job key '{KeyHelper.GetKeyTitle(metadata.JobKey)}'");
        await ValidateJobNotRunning(metadata.JobKey);
    }
}
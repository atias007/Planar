﻿using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pipelines.Sockets.Unofficial.Arenas;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Common.Exceptions;
using Planar.Service.API.Helpers;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Planar.Service.Model;
using Planar.Service.Validation;
using Quartz;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Planar.Service.API;

public partial class JobDomain
{
    private const int MaxNameLength = 50;

    private const int MinNameLength = 3;

    private const string NameRegexTemplate = @"^[a-zA-Z0-9\-_\s]{@MinNameLength@,@MaxNameLength@}$";

    private static readonly string[] _cronValues = ["auto", "donothing", "fireandproceed", "ignoremisfires"];

    private static readonly Regex _regex = new(
            NameRegexTemplate
            .Replace("@MinNameLength@", MinNameLength.ToString())
            .Replace("@MaxNameLength@", MaxNameLength.ToString()), RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    private static readonly string[] _simpleValues = ["auto", "firenow", "ignoremisfires", "nextwithexistingcount", "nextwithremainingcount", "nowwithexistingcount", "nowwithremainingcount"];
    private static readonly DateTimeOffset DelayStartTriggerDateTime = new(DateTime.Now.AddSeconds(3));

    public static void ValidateDataMap(Dictionary<string, string?>? data, string title)
    {
        if (data == null) { return; }

        var dataCount = CountUserJobDataItems(data);
        if (dataCount > Consts.MaximumJobDataItems)
        {
            throw new RestValidationException("key", $"{title} data has more then {Consts.MaximumJobDataItems} items ({data.Count})".Trim());
        }

        if (data.Any(item => string.IsNullOrWhiteSpace(item.Key)))
        {
            throw new RestValidationException("key", $"{title} data key must have value".Trim());
        }

        foreach (var item in data)
        {
            ValidateRange(item.Key, 1, 200, "key", $"{title} data".Trim());
            ValidateMaxLength(item.Value, 1000, "value", $"{title} data".Trim());
        }

        var invalidKeys = data
                .Where(item => !Consts.IsDataKeyValid(item.Key))
                .Select(item => item.Key)
                .ToList();

        if (invalidKeys.Count > 0)
        {
            var keys = string.Join(',', invalidKeys);
            throw new RestValidationException("key", $"{title} data key(s) '{keys}' is invalid");
        }
    }

    public async Task<PlanarIdResponse> Add(SetJobPathRequest request)
    {
        await ValidateAddPath(request);
        var yml = await GetJobFileContent(request);
        var dynamicRequest = GetJobDynamicRequest(yml);
        dynamic properties = dynamicRequest.Properties ?? new ExpandoObject();
        var path = ConvertRelativeJobFileToRelativeJobPath(request);
        properties["path"] = path;
        var response = await Add(dynamicRequest);
        return response;
    }

    private static void AddAuthor(SetJobRequest metadata, IJobDetail job)
    {
        if (string.IsNullOrEmpty(metadata.Author)) { return; }
        job.JobDataMap.Put(Consts.Author, metadata.Author);
    }

    private static void AddCircuitBreaker(SetJobRequest metadata, IJobDetail job)
    {
        if (!metadata.CircuitBreaker.Enabled) { return; }
        job.JobDataMap.Put(Consts.CircuitBreaker, metadata.CircuitBreaker.ToString());
    }

    private static void AddLogRetentionDays(SetJobRequest metadata, IJobDetail job)
    {
        if (metadata.LogRetentionDays == null) { return; }
        job.JobDataMap.Put(Consts.LogRetentionDays, metadata.LogRetentionDays.Value.ToString());
    }

    private static void BuidCronSchedule(CronScheduleBuilder builder, JobCronTriggerMetadata trigger)
    {
        if (string.IsNullOrWhiteSpace(trigger.MisfireBehaviour)) { return; }

        var value = trigger.MisfireBehaviour.ToLower().Replace(" ", string.Empty);
        switch (value)
        {
            case "donothing":
                builder.WithMisfireHandlingInstructionDoNothing();
                break;

            case "fireandproceed":
                builder.WithMisfireHandlingInstructionFireAndProceed();
                break;

            case "ignoremisfires":
                builder.WithMisfireHandlingInstructionIgnoreMisfires();
                break;

            default:
                break;
        }
    }

    private static void BuildSimpleSchedule(SimpleScheduleBuilder builder, JobSimpleTriggerMetadata trigger)
    {
        // Interval
        builder = builder.WithInterval(trigger.Interval);

        // Repeat
        if (trigger.RepeatCount.HasValue)
        {
            builder = builder.WithRepeatCount(trigger.RepeatCount.Value);
        }
        else
        {
            builder = builder.RepeatForever();
        }

        // MisfireBehaviour
        if (!string.IsNullOrEmpty(trigger.MisfireBehaviour))
        {
            var value = trigger.MisfireBehaviour.ToLower().Replace(" ", string.Empty);
            switch (value)
            {
                case "firenow":
                    builder.WithMisfireHandlingInstructionFireNow();
                    break;

                case "ignoremisfires":
                    builder.WithMisfireHandlingInstructionIgnoreMisfires();
                    break;

                case "nextwithexistingcount":
                    builder.WithMisfireHandlingInstructionNextWithExistingCount();
                    break;

                case "nextwithremainingcount":
                    builder.WithMisfireHandlingInstructionNextWithRemainingCount();
                    break;

                case "nowwithexistingcount":
                    builder.WithMisfireHandlingInstructionNowWithExistingCount();
                    break;

                case "nowwithremainingcount":
                    builder.WithMisfireHandlingInstructionNowWithRemainingCount();
                    break;

                case "auto":
                default:
                    break;
            }
        }
    }

    private static void BuildJobData(SetJobRequest metadata, IJobDetail job)
    {
        if (metadata.JobData == null) { return; }
        foreach (var item in metadata.JobData)
        {
            job.JobDataMap.Put(item.Key, item.Value);
        }
    }

    // JobType+Concurrent, JobGroup, JobName, Description, Durable
    private static IJobDetail BuildJobDetails(SetJobDynamicRequest request, JobKey jobKey)
    {
        var jobType = GetJobType(request);
        var jobBuilder = JobBuilder.Create(jobType)
            .WithIdentity(jobKey)
            .WithDescription(request.Description)
            .RequestRecovery();

        if (request.Durable.GetValueOrDefault())
        {
            jobBuilder = jobBuilder.StoreDurably(true);
        }

        var job = jobBuilder.Build();
        return job;
    }

    private static List<ITrigger> BuildTriggers(SetJobRequest job, string jobId)
    {
        var quartzTriggers1 = BuildTriggerWithSimpleSchedule(job.SimpleTriggers, jobId);
        var quartzTriggers2 = BuildTriggerWithCronSchedule(job.CronTriggers, jobId);
        var allTriggers = new List<ITrigger>();
        if (quartzTriggers1 != null) { allTriggers.AddRange(quartzTriggers1); }
        if (quartzTriggers2 != null) { allTriggers.AddRange(quartzTriggers2); }
        return allTriggers;
    }

    private static IEnumerable<ITrigger> BuildTriggerWithCronSchedule(List<JobCronTriggerMetadata> triggers, string jobId)
    {
        if (triggers.IsNullOrEmpty()) { return []; }

        var result = triggers.Select(t =>
        {
            var trigger = GetBaseTriggerBuilder(t, jobId)
                .WithCronSchedule(t.CronExpression, c => BuidCronSchedule(c, t));

            return trigger.Build();
        });

        return result;
    }

    private static IEnumerable<ITrigger> BuildTriggerWithSimpleSchedule(List<JobSimpleTriggerMetadata> triggers, string jobId)
    {
        if (triggers.IsNullOrEmpty()) { return []; }

        var result = triggers.Select(t =>
        {
            var trigger = GetBaseTriggerBuilder(t, jobId);

            if (t.Start == null)
            {
                trigger = trigger.StartAt(DelayStartTriggerDateTime);
            }
            else
            {
                trigger = trigger.StartAt(new DateTimeOffset(t.Start.Value));
            }

            if (t.End != null)
            {
                trigger = trigger.EndAt(new DateTimeOffset(t.End.Value));
            }

            trigger = trigger.WithSimpleSchedule(s => BuildSimpleSchedule(s, t));

            return trigger.Build();
        });

        return result;
    }

    private static string ConvertRelativeJobFileToRelativeJobPath(IJobFileRequest request)
    {
        var jobsPath = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs);
        var fullname = Path.Combine(jobsPath, request.JobFilePath);
        var jobDir = new FileInfo(fullname).Directory?.FullName;
        var path = ServiceUtil.GetJobRelativePath(jobDir);
        return path;
    }

    private static string CreateJobId(IJobDetail job)
    {
        // job id
        var id = ServiceUtil.GenerateId();
        job.JobDataMap.Add(Consts.JobId, id);

        return id ?? string.Empty;
    }

    private static TriggerBuilder GetBaseTriggerBuilder(BaseTrigger jobTrigger, string jobId)
    {
        var id =
            string.IsNullOrEmpty(jobTrigger.Id) ?
            ServiceUtil.GenerateId() :
            jobTrigger.Id;

        var trigger = TriggerBuilder.Create();
        jobTrigger.Group = jobId;
        trigger = trigger.WithIdentity(jobTrigger.Name ?? string.Empty, jobTrigger.Group);

        // Priority
        if (jobTrigger.Priority.HasValue)
        {
            trigger = trigger.WithPriority(jobTrigger.Priority.Value);
        }

        // Calendar
        if (jobTrigger.Calendar.HasValue())
        {
            trigger = trigger.ModifiedByCalendar(jobTrigger.Calendar);
        }

        // Data
        jobTrigger.TriggerData ??= [];

        if (jobTrigger.TriggerData.Count > 0)
        {
            trigger = trigger.UsingJobData(new JobDataMap(jobTrigger.TriggerData));
        }

        // Data --> TriggerId
        trigger = trigger.UsingJobData(Consts.TriggerId, id);

        // Data --> TriggerTimeout
        if (jobTrigger.Timeout.HasValue)
        {
            var timeoutValue = jobTrigger.Timeout.Value.Ticks.ToString();
            trigger = trigger.UsingJobData(Consts.TriggerTimeout, timeoutValue);
        }

        // Data --> Retry span, Max retries
        if (jobTrigger.RetrySpan.HasValue)
        {
            trigger = trigger.UsingJobData(Consts.RetrySpan, jobTrigger.RetrySpan.Value.ToSimpleTimeString());
        }

        // Data --> Max retries
        if (jobTrigger.MaxRetries.HasValue)
        {
            trigger = trigger.UsingJobData(Consts.MaxRetries, jobTrigger.MaxRetries.Value.ToString());
        }

        return trigger;
    }

    private static SetJobDynamicRequest GetJobDynamicRequest(string yml)
    {
        SetJobDynamicRequest dynamicRequest;

        try
        {
            dynamicRequest = YmlUtil.Deserialize<SetJobDynamicRequest>(yml);
        }
        catch (Exception ex)
        {
            throw new RestValidationException("path", $"fail to read JobFile.yml. error: {ex.Message}");
        }

        return dynamicRequest;
    }

    private static string GetJobFileFullName(IJobFileRequest request)
    {
        if (request.JobFilePath == null) { return string.Empty; }
        var filename = ServiceUtil.GetJobFilename(null, request.JobFilePath);
        return filename;
    }

    private static Type GetJobType(SetJobRequest job)
    {
        string typeName;
        Assembly assembly;

        try
        {
            if (job.JobType == null) { return typeof(object); }
            assembly = Assembly.Load(job.JobType);
        }
        catch (Exception ex)
        {
            throw new RestValidationException("jobType", $"fail to load assemly {job.JobType} ({ex.Message})");
        }

        if (job.Concurrent)
        {
            typeName = $"Planar.{job.JobType}Concurrent";
        }
        else
        {
            typeName = $"Planar.{job.JobType}NoConcurrent";
        }

        try
        {
            var type = assembly.GetType(typeName);
            return type ?? throw new RestValidationException("jobType", $"type {typeName} is not supported");
        }
        catch (Exception ex)
        {
            throw new RestValidationException("jobType", $"fail to get type {job.JobType} from assemly {assembly.FullName} ({ex.Message})");
        }
    }

    private static string? GetJopPropertiesYml(SetJobDynamicRequest request)
    {
        if (request.Properties == null)
        {
            return null;
        }

        var yml = YmlUtil.Serialize(request.Properties);
        return yml;
    }

    private static bool IsRegexMatch(Regex regex, string? value)
    {
        if (value == null) { return true; }
        return regex.IsMatch(value);
    }

    private static void TrimTriggerProperties(ITriggersContainer container)
    {
        container.SimpleTriggers?.ForEach(t =>
        {
            t.Name = t.Name.SafeTrim();
            t.Group = t.Group.SafeTrim();
            t.Calendar = t.Calendar.SafeTrim();
        });
        container.CronTriggers?.ForEach(t =>
        {
            t.Name = t.Name.SafeTrim();
            t.Group = t.Group.SafeTrim();
            t.Calendar = t.Calendar.SafeTrim();
        });
    }

    private static void ValidateCronExpression(ITriggersContainer container)
    {
        container.CronTriggers?.ForEach(t =>
        {
            if (string.IsNullOrEmpty(t.CronExpression)) { throw new RestValidationException("cron expression", "cron expression is mandatory in cron trigger"); }
            if (!ValidationUtil.IsValidCronExpression(t.CronExpression)) { throw new RestValidationException("cron expression", $"cron expression '{t.CronExpression}' is invalid"); }
        });
    }

    private static JobKey ValidateJobMetadata(SetJobRequest metadata, IScheduler scheduler)
    {
        metadata.JobData ??= [];

        #region Trim

        metadata.Name = metadata.Name.SafeTrim();
        metadata.Group = metadata.Group.SafeTrim();
        metadata.Description = metadata.Description.SafeTrim();
        metadata.JobType = metadata.JobType.SafeTrim();

        #endregion Trim

        #region Mandatory

        if (string.IsNullOrWhiteSpace(metadata.Name)) throw new RestValidationException("name", "job name is mandatory");
        if (string.IsNullOrWhiteSpace(metadata.JobType)) throw new RestValidationException("type", "job type is mandatory");

        #endregion Mandatory

        #region JobType

        if (!ServiceUtil.JobTypes.Contains(metadata.JobType))
        {
            throw new RestValidationException("job type", $"job type '{metadata.JobType}' is not supported");
        }

        #endregion JobType

        #region Valid Name & Group

        if (!IsRegexMatch(_regex, metadata.Name))
        {
            throw new RestValidationException("name", $"job name '{metadata.Name}' is invalid. use only alphanumeric, dashes & underscore");
        }

        if (!IsRegexMatch(_regex, metadata.Group))
        {
            throw new RestValidationException("group", $"job group '{metadata.Group}' is invalid. use only alphanumeric, dashes & underscore");
        }

        if (Consts.PreserveGroupNames.Contains(metadata.Group))
        {
            throw new RestValidationException("group", $"job group '{metadata.Group}' is invalid (preserved value)");
        }

        #endregion Valid Name & Group

        #region Max Chars / Value

        ValidateRange(metadata.Name, 5, 50, "name", "job");
        ValidateRange(metadata.Group, 1, 50, "group", "job");
        ValidateRangeValue(metadata.LogRetentionDays, 1, 1000, "log retention days", "job");
        ValidateMaxLength(metadata.Author, 200, "author", "job");
        ValidateMaxLength(metadata.Description, 100, "description", "job");

        if (metadata.LogRetentionDays.GetValueOrDefault() > AppSettings.Retention.JobLogRetentionDays)
        {
            throw new RestValidationException("log retention days", $"log retention days can not be greater than {AppSettings.Retention.JobLogRetentionDays} (global app settings)");
        }

        #endregion Max Chars / Value

        #region JobData

        if (metadata.JobData != null && metadata.JobData.Count != 0 && metadata.Concurrent)
        {
            throw new RestValidationException("concurrent", $"job with concurrent=true can not have data. persist data with concurrent running may cause unexpected results");
        }

        ValidateDataMap(metadata.JobData, "job");

        #endregion JobData

        #region circuit breaker

        if (metadata.CircuitBreaker.Enabled)
        {
            ValidateRangeValue(metadata.CircuitBreaker.FailureThreshold, 2, 100, "failure threshold", "circuit breaker");
            ValidateRangeValue(metadata.CircuitBreaker.SuccessThreshold, 1, 100, "success threshold", "circuit breaker");
            ValidateMinMinutes(metadata.CircuitBreaker.PauseSpan, 5, "span value", "circuit breaker");

            if (metadata.CircuitBreaker.SuccessThreshold >= metadata.CircuitBreaker.FailureThreshold)
            {
                throw new RestValidationException(
                    nameof(metadata.CircuitBreaker.SuccessThreshold).ToLower(),
                    $"circuit breaker success threshold value is invalid. success threshold must be less than failure threshold");
            }
        }

        #endregion circuit breaker

        var triggersCount = metadata.CronTriggers?.Count + metadata.SimpleTriggers?.Count;
        if (triggersCount == 0 && metadata.Durable == false)
        {
            throw new RestValidationException("durable", $"job without any trigger must be durable. set the durable property to true or add at least one trigger");
        }

        ValidateTriggerMetadata(metadata, scheduler);

        var jobKey =
            JobKeyHelper.GetJobKey(metadata) ??
            throw new RestGeneralException($"fail to create job key from job group '{metadata.Group}' and job name '{metadata.Name}'");

        return jobKey;
    }

    private static void ValidateMandatoryTriggerProperties(ITriggersContainer container)
    {
        container.SimpleTriggers?.ForEach(t =>
        {
            t.TriggerData ??= [];
            if (string.IsNullOrEmpty(t.Name)) throw new RestValidationException("name", "trigger name is mandatory");

            var emptyKeys = t.TriggerData.Any(item => string.IsNullOrWhiteSpace(item.Key));
            if (emptyKeys) throw new RestValidationException("key", "trigger data key must have value");
        });
        container.CronTriggers?.ForEach(t =>
        {
            t.TriggerData ??= [];
            if (string.IsNullOrEmpty(t.Name)) throw new RestValidationException("name", "trigger name is mandatory");
        });
    }

    private static void ValidateMaxCharsTiggerProperties(TriggerPool pool)
    {
        foreach (var t in pool.Triggers)
        {
            t.TriggerData ??= [];
            ValidateRange(t.Name, 5, 50, "name", "trigger");
            ValidateRange(t.Group, 1, 50, "group", "trigger");
            ValidateMaxLength(t.Calendar, 50, "calendar", "trigger");
            ValidateRangeValue(t.MaxRetries, 1, 100, "max retries", "trigger");

            foreach (var item in t.TriggerData)
            {
                ValidateRange(item.Key, 1, 100, "key", "trigger data");
                ValidateMaxLength(item.Value, 1000, "value", "trigger data");
            }
        }
    }

    private static void ValidatePreserveWordsTriggerProperties(TriggerPool pool)
    {
        foreach (var t in pool.Triggers)
        {
            t.TriggerData ??= [];
            if (Consts.PreserveGroupNames.Contains(t.Group)) { throw new RestValidationException("group", $"trigger group '{t.Group}' is invalid (preserved value)"); }
            if (t.Name != null && t.Name.StartsWith(Consts.RetryTriggerNamePrefix)) { throw new RestValidationException("name", $"simple trigger name '{t.Name}' has invalid prefix"); }
            ValidateDataMap(t.TriggerData, "trigger");
        }
    }

    private static void ValidateRequestNoNull(object request)
    {
        if (request == null)
        {
            throw new RestValidationException("request", "request is null");
        }
    }

    private static void ValidateTriggerCalendar(TriggerPool pool, IScheduler scheduler)
    {
        var calendars = scheduler.GetCalendarNames().Result;

        foreach (var t in pool.Triggers)
        {
            if (string.IsNullOrEmpty(t.Calendar)) { continue; }
            var existsCalendarName = calendars.FirstOrDefault(c => string.Equals(c, t.Calendar, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(existsCalendarName))
            {
                throw new RestValidationException(nameof(t.Calendar), $"calendar name '{t.Calendar}' is not exists");
            }

            t.Calendar = existsCalendarName;
        }
    }

    private static void ValidateTriggerInterval(ITriggersContainer container)
    {
        container.SimpleTriggers?.ForEach(t =>
        {
            if (t.RepeatCount >= 0 && t.Interval.TotalSeconds < 60) { throw new RestValidationException("interval", $"interval has invalid value. interval must be greater or equals to 1 minute"); }
        });
    }

    private static void ValidateTriggerMetadata(ITriggersContainer container, IScheduler scheduler)
    {
        var pool = new TriggerPool(container);
        TrimTriggerProperties(container);
        ValidateMandatoryTriggerProperties(container);
        ValidateTriggerNameProperties(pool);
        ValidateMaxCharsTiggerProperties(pool);
        ValidatePreserveWordsTriggerProperties(pool);
        ValidateTriggerPriority(pool);
        ValidateTriggerTimeout(pool);
        ValidateTriggerInterval(container);
        ValidateTriggerRepeatCount(container);
        ValidateTriggerRetry(pool);
        ValidateTriggerStartEnd(container);
        ValidateCronExpression(container);
        ValidateTriggerMisfireBehaviour(container);
        ValidateTriggerCalendar(pool, scheduler);
    }

    private static void ValidateTriggerMisfireBehaviour(ITriggersContainer container)
    {
        container.SimpleTriggers?.ForEach(t =>
        {
            if (t.MisfireBehaviour.HasValue() && _simpleValues.NotContains(t.MisfireBehaviour?.ToLower()?.Replace(" ", string.Empty)))
            {
                throw new RestValidationException("misfire behaviour", $"value {t.MisfireBehaviour} is not valid value for simple trigger misfire behaviour");
            }
        });

        container.CronTriggers?.ForEach(t =>
        {
            if (t.MisfireBehaviour.HasValue() && _cronValues.NotContains(t.MisfireBehaviour?.ToLower()?.Replace(" ", string.Empty)))
            {
                throw new RestValidationException("misfire behaviour", $"value {t.MisfireBehaviour} is not valid value for cron trigger misfire behaviour");
            }
        });
    }

    private static void ValidateTriggerNameProperties(TriggerPool pool)
    {
        foreach (var t in pool.Triggers)
        {
            if (!IsRegexMatch(_regex, t.Name)) throw new RestValidationException("name", $"trigger name '{t.Name}' is invalid. use only alphanumeric, dashes & underscore");
            if (!IsRegexMatch(_regex, t.Group)) throw new RestValidationException("group", $"trigger group '{t.Group}' is invalid. use only alphanumeric, dashes & underscore");
        }
    }

    private static void ValidateTriggerPriority(TriggerPool pool)
    {
        foreach (var t in pool.Triggers)
        {
            if (t.Priority < 0 || t.Priority > 100) { throw new RestValidationException("priority", $"priority has invalid value. priority must be between 0 to 100"); }
        }
    }

    private static void ValidateTriggerRepeatCount(ITriggersContainer container)
    {
        container.SimpleTriggers?.ForEach(t =>
        {
            if (t.RepeatCount < 0) { throw new RestValidationException("repeat count", "repeat count has invalid value. repeat count must be greater to 1"); }
        });
    }

    private static void ValidateTriggerRetry(TriggerPool pool)
    {
        foreach (var t in pool.Triggers)
        {
            if (t.MaxRetries < 1 || t.MaxRetries > 100) { throw new RestValidationException("max retries", $"max retries has invalid value. max retries must be between 1 to 100"); }

            if ((t.RetrySpan == null || t.RetrySpan == TimeSpan.Zero) && t.MaxRetries > 0) { throw new RestValidationException("retry span", $"retry span has invalid value. retry span must have value when max retries has value"); }
            if (t.RetrySpan.HasValue && t.RetrySpan.Value.TotalSeconds < 1) { throw new RestValidationException("retry span", $"retry span has invalid value. retry span must be greater or equals to 1 seconds"); }
            if (t.RetrySpan.HasValue && t.RetrySpan.Value.TotalDays > 1) { throw new RestValidationException("retry span", $"retry span has invalid value. retry span must be less then or equals to 1 day"); }
        }
    }

    private static void ValidateTriggerStartEnd(ITriggersContainer container)
    {
        container.SimpleTriggers?.ForEach(t =>
        {
            if (t.Start.HasValue && t.Start.Value.Date < DateTime.Now)
            {
                t.Start = DateTime.Now.Date.Add(t.Start.Value.TimeOfDay);
                if (t.Start < DateTime.Now)
                {
                    var interval = (int)Math.Floor(t.Interval.TotalSeconds);
                    var delta = Math.Floor(DateTimeOffset.UtcNow.Subtract(t.Start.Value.ToUniversalTime()).TotalSeconds);
                    var steps = (int)Math.Ceiling(delta / interval);
                    var increase = steps * interval;
                    t.Start = t.Start.Value.AddSeconds(increase);
                }
            }

            if (t.Start.HasValue && t.End.HasValue && t.Start.Value >= t.End.Value)
            {
                throw new RestValidationException("end", $"end time has invalid value. end time cannot be before/equals start time");
            }

            if (t.End.HasValue && t.End.Value <= DateTime.Now)
            {
                throw new RestValidationException("end", $"end time has invalid value. end time cannot be before/equals current server time");
            }
        });
    }

    private static void ValidateTriggerTimeout(TriggerPool pool)
    {
        foreach (var t in pool.Triggers)
        {
            if (t.Timeout.HasValue && t.Timeout.Value.TotalMinutes < 1) { throw new RestValidationException("timeout", $"timeout has invalid value. timeout must be greater or equals to 1 minute"); }
            if (t.Timeout.HasValue && t.Timeout.Value.TotalDays > 1) { throw new RestValidationException("timeout", $"timeout has invalid value. timeout must be less then or equals to 1 day"); }
        }
    }

    private async Task<PlanarIdResponse> Add(SetJobDynamicRequest request)
    {
        // Validation
        ValidateRequestNoNull(request);
        await ValidateRequestProperties(request);
        var jobKey = ValidateJobMetadata(request, Scheduler);
        await ValidateJobNotExists(jobKey);

        // Create Job (JobType+Concurrent, JobGroup, JobName, Description, Durable)
        var job = BuildJobDetails(request, jobKey);

        // Add: Author, CircuitBreaker, RetentionDays
        AddAuthor(request, job);
        AddCircuitBreaker(request, job);
        AddLogRetentionDays(request, job);

        // Build Data
        BuildJobData(request, job);

        // Create Job Id
        var id = CreateJobId(job);

        // Build Triggers
        var triggers = BuildTriggers(request, id);

        // Save Job Properties
        var jobPropertiesYml = GetJopPropertiesYml(request);
        var jobType = SchedulerUtil.GetJobTypeName(job);
        var property = new JobProperty { JobId = id, Properties = jobPropertiesYml, JobType = jobType };
        await DataLayer.AddJobProperty(property);

        try
        {
            // Schedule Job
            await Scheduler.ScheduleJob(job, triggers, true);
        }
        catch (Exception ex)
        {
            ValidateTriggerNeverFire(ex);

            // roll back
            await DataLayer.DeleteJobProperty(id);
            throw;
        }

        AuditJobSafe(jobKey, "job added", request);

        // Return Id
        return new PlanarIdResponse { Id = id };
    }

    private async Task<string> GetJobFileContent(IJobFileRequest request)
    {
        await ValidateAddPath(request);
        string yml;
        var filename = GetJobFileFullName(request);
        try
        {
            yml = await File.ReadAllTextAsync(filename);
        }
        catch (Exception ex)
        {
            throw new RestGeneralException($"fail to read file: {filename}", ex);
        }

        return yml;
    }

    private async Task ValidateAddPath(IJobFileRequest request)
    {
        ValidateRequestNoNull(request);

        ////try
        ////{
        ////    ServiceUtil.ValidateJobFolderExists(request.JobFileInfo.Directory.FullName);
        ////    var util = _serviceProvider.GetRequiredService<ClusterUtil>();
        ////    await util.ValidateJobFolderExists(request.Folder);
        ////}
        ////catch (PlanarException ex)
        ////{
        ////    throw new RestValidationException("folder", ex.Message);
        ////}

        try
        {
            ServiceUtil.ValidateJobFileExists(request.JobFilePath);
            var util = ServiceProvider.GetRequiredService<ClusterUtil>();
            await util.ValidateJobFileExists(null, request.JobFilePath);
        }
        catch (PlanarException ex)
        {
            throw new RestValidationException("folder", ex.Message);
        }
    }

    private async Task ValidateJobNotExists(JobKey jobKey)
    {
        var exists = await Scheduler.GetJobDetail(jobKey);

        if (exists != null)
        {
            throw new RestConflictException($"job with name: {jobKey.Name} and group: {jobKey.Group} already exists");
        }
    }

    private async Task ValidateJobProperties<TProperties>(string? yml)
        where TProperties : class
    {
        if (yml == null)
        {
            throw new RestValidationException("properties", "properties is null or empty");
        }

        var properties = YmlUtil.Deserialize<TProperties>(yml) ??
            throw new RestValidationException("properties", "properties is null or empty");

        var validator = ServiceProvider.GetService<IValidator<TProperties>>();

        if (validator == null)
        {
            Logger.LogWarning("Job properties of type {PropertyType} has no registered validation in DI. validation skipped", typeof(TProperties).FullName);
            return;
        }

        await validator.ValidateAndThrowAsync(properties);
    }

    private async Task ValidatePropertiesInner(SetJobDynamicRequest request)
    {
        var yml = GetJopPropertiesYml(request);

        switch (request.JobType)
        {
            case nameof(PlanarJob):
                await ValidateJobProperties<PlanarJobProperties>(yml);
                break;

            case nameof(ProcessJob):
                await ValidateJobProperties<ProcessJobProperties>(yml);
                break;

            case nameof(SqlJob):
                await ValidateJobProperties<SqlJobProperties>(yml);
                break;

            case nameof(RestJob):
                await ValidateJobProperties<RestJobProperties>(yml);
                break;

            case nameof(SqlTableReportJob):
                await ValidateJobProperties<SqlTableReportJobProperties>(yml);
                break;

            case nameof(SequenceJob):
                await ValidateJobProperties<SequenceJobProperties>(yml);
                break;

            default:
                Logger.LogError("Missing validation for job type {JobType} at ValidatePropertiesInner", request.JobType);
                break;
        }
    }

    private async Task ValidateRequestProperties(SetJobDynamicRequest request)
    {
        try
        {
            await ValidatePropertiesInner(request);
        }
        catch (Exception ex)
        {
            throw new RestValidationException("properties", $"fail to read/validate properties section. error: {ex.Message}");
        }
    }

    private struct TriggerPool
    {
        public TriggerPool(ITriggersContainer container)
        {
            var temp = new List<BaseTrigger>();
            temp.AddRange(container.SimpleTriggers);
            temp.AddRange(container.CronTriggers);
            Triggers = temp;
        }

        public IEnumerable<BaseTrigger> Triggers { get; private set; }
    }
}
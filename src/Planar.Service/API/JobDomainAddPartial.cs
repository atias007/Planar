using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
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

namespace Planar.Service.API
{
    public partial class JobDomain
    {
        private const int MaxNameLength = 50;

        private const int MinNameLength = 3;

        private const string NameRegexTemplate = @"^[a-zA-Z0-9\-_\s]{@MinNameLength@,@MaxNameLength@}$";

        private static readonly Regex _regex = new(
            NameRegexTemplate
                .Replace("@MinNameLength@", MinNameLength.ToString())
                .Replace("@MaxNameLength@", MaxNameLength.ToString()), RegexOptions.Compiled, TimeSpan.FromSeconds(5));

        private static readonly DateTimeOffset DelayStartTriggerDateTime = new(DateTime.Now.AddSeconds(3));

        public static IEnumerable<ITrigger> BuildTriggerWithCronSchedule(List<JobCronTriggerMetadata> triggers, string jobId)
        {
            if (triggers.IsNullOrEmpty()) { return new List<ITrigger>(); }

            var result = triggers.Select(t =>
            {
                var trigger = GetBaseTriggerBuilder(t, jobId)
                    .WithCronSchedule(t.CronExpression, c => BuidCronSchedule(c, t));

                return trigger.Build();
            });

            return result;
        }

        public static IEnumerable<ITrigger> BuildTriggerWithSimpleSchedule(List<JobSimpleTriggerMetadata> triggers, string jobId)
        {
            if (triggers.IsNullOrEmpty()) { return new List<ITrigger>(); }

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

                trigger = trigger.WithSimpleSchedule(s => BuidSimpleSchedule(s, t));

                return trigger.Build();
            });

            return result;
        }

        public static void ValidateTriggerMetadata(ITriggersContainer container, IScheduler scheduler)
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

        public async Task<JobIdResponse> AddByPath(SetJobPathRequest request)
        {
            await ValidateAddPath(request);
            var yml = await GetJobFileContent(request);
            var dynamicRequest = GetJobDynamicRequest(yml);
            dynamic properties = dynamicRequest.Properties ?? new ExpandoObject();
            properties["path"] = request.Folder;
            var response = await Add(dynamicRequest);
            return response;
        }

        private static void AddAuthor(SetJobRequest metadata, IJobDetail job)
        {
            if (string.IsNullOrEmpty(metadata.Author)) { return; }
            job.JobDataMap.Put(Consts.Author, metadata.Author);
        }

        private static void AddLogRetentionDays(SetJobRequest metadata, IJobDetail job)
        {
            if (metadata.LogRetentionDays == null) { return; }
            job.JobDataMap.Put(Consts.LogRetentionDays, metadata.LogRetentionDays.Value.ToString());
        }

        private static void BuidCronSchedule(CronScheduleBuilder builder, JobCronTriggerMetadata trigger)
        {
            if (!string.IsNullOrEmpty(trigger.MisfireBehaviour))
            {
                var value = trigger.MisfireBehaviour.ToLower();
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
        }

        private static void BuidSimpleSchedule(SimpleScheduleBuilder builder, JobSimpleTriggerMetadata trigger)
        {
            builder = builder.WithInterval(trigger.Interval);
            if (trigger.RepeatCount.HasValue)
            {
                builder = builder.WithRepeatCount(trigger.RepeatCount.Value);
            }
            else
            {
                builder = builder.RepeatForever();
            }

            if (!string.IsNullOrEmpty(trigger.MisfireBehaviour))
            {
                var value = trigger.MisfireBehaviour.ToLower();
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

                    default:
                        break;
                }
            }
        }

        private static void BuildJobData(SetJobRequest metadata, IJobDetail job)
        {
            if (metadata.JobData != null)
            {
                foreach (var item in metadata.JobData)
                {
                    job.JobDataMap.Put(item.Key, item.Value);
                }
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

        private static IReadOnlyCollection<ITrigger> BuildTriggers(SetJobRequest job, string jobId)
        {
            var quartzTriggers1 = BuildTriggerWithSimpleSchedule(job.SimpleTriggers, jobId);
            var quartzTriggers2 = BuildTriggerWithCronSchedule(job.CronTriggers, jobId);
            var allTriggers = new List<ITrigger>();
            if (quartzTriggers1 != null) { allTriggers.AddRange(quartzTriggers1); }
            if (quartzTriggers2 != null) { allTriggers.AddRange(quartzTriggers2); }
            return allTriggers;
        }

        private static void CheckForInvalidDataKeys(Dictionary<string, string?>? data, string title)
        {
            if (data == null) { return; }

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
            jobTrigger.TriggerData ??= new Dictionary<string, string?>();

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

        private static string GetJobFileFullName(SetJobPathRequest request)
        {
            if (request.JobFileName == null) { return string.Empty; }
            var filename = ServiceUtil.GetJobFilename(request.Folder, request.JobFileName);
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
                if (string.IsNullOrEmpty(t.CronExpression)) { throw new RestValidationException("priority", "cron expression is mandatory in cron trigger"); }
            });
        }

        private static JobKey ValidateJobMetadata(SetJobRequest metadata, IScheduler scheduler)
        {
            metadata.JobData ??= new Dictionary<string, string?>();

            #region Trim

            metadata.Name = metadata.Name.SafeTrim();
            metadata.Group = metadata.Group.SafeTrim();
            metadata.Description = metadata.Description.SafeTrim();
            metadata.JobType = metadata.JobType.SafeTrim();

            #endregion Trim

            #region Mandatory

            if (string.IsNullOrWhiteSpace(metadata.Name)) throw new RestValidationException("name", "job name is mandatory");
            if (string.IsNullOrWhiteSpace(metadata.JobType)) throw new RestValidationException("type", "job type is mandatory");

            if (metadata.JobData.Any(item => string.IsNullOrWhiteSpace(item.Key)))
            {
                throw new RestValidationException("key", "job data key must have value");
            }

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

            foreach (var item in metadata.JobData)
            {
                ValidateRange(item.Key, 1, 100, "key", "job data");
                ValidateMaxLength(item.Value, 1000, "value", "job data");
            }

            if (metadata.LogRetentionDays.GetValueOrDefault() > AppSettings.ClearJobLogTableOverDays)
            {
                throw new RestValidationException("log retention days", $"log retention days can not be greater than {AppSettings.ClearJobLogTableOverDays} (global app settings)");
            }

            #endregion Max Chars / Value

            #region JobData

            if (metadata.JobData != null && metadata.JobData.Any() && metadata.Concurrent)
            {
                throw new RestValidationException("concurrent", $"job with concurrent=true can not have data. persist data with concurrent running may cause unexpected results");
            }

            CheckForInvalidDataKeys(metadata.JobData, "job");

            #endregion JobData

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
                t.TriggerData ??= new Dictionary<string, string?>();
                if (string.IsNullOrEmpty(t.Name)) throw new RestValidationException("name", "trigger name is mandatory");

                var emptyKeys = t.TriggerData.Any(item => string.IsNullOrWhiteSpace(item.Key));
                if (emptyKeys) throw new RestValidationException("key", "trigger data key must have value");
            });
            container.CronTriggers?.ForEach(t =>
            {
                t.TriggerData ??= new Dictionary<string, string?>();
                if (string.IsNullOrEmpty(t.Name)) throw new RestValidationException("name", "trigger name is mandatory");
            });
        }

        private static void ValidateMaxCharsTiggerProperties(TriggerPool pool)
        {
            foreach (var t in pool.Triggers)
            {
                t.TriggerData ??= new Dictionary<string, string?>();
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

        private static void ValidateMaxLength(string? value, int length, string name, string parent)
        {
            if (value != null && value.Length > length)
            {
                throw new RestValidationException(name, $"{parent} {name} length is invalid. maximum length is {length}");
            }
        }

        private static void ValidateMaxValue(int? value, int to, string name, string parent)
        {
            if (value != null && value > to)
            {
                throw new RestValidationException(name, $"{parent} {name} value is invalid. maximum value is {to}");
            }
        }

        private static void ValidateMinLength(string? value, int length, string name, string parent)
        {
            if (value != null && value.Length < length)
            {
                throw new RestValidationException(name, $"{parent} {name} length is invalid. minimum length is {length}");
            }
        }

        private static void ValidateMinValue(int? value, int from, string name, string parent)
        {
            if (value != null && value < from)
            {
                throw new RestValidationException(name, $"{parent} {name} value is invalid. minimum value is {from}");
            }
        }

        private static void ValidatePreserveWordsTriggerProperties(TriggerPool pool)
        {
            foreach (var t in pool.Triggers)
            {
                t.TriggerData ??= new Dictionary<string, string?>();
                if (Consts.PreserveGroupNames.Contains(t.Group)) { throw new RestValidationException("group", $"trigger group '{t.Group}' is invalid (preserved value)"); }
                if (t.Name != null && t.Name.StartsWith(Consts.RetryTriggerNamePrefix)) { throw new RestValidationException("name", $"simple trigger name '{t.Name}' has invalid prefix"); }
                CheckForInvalidDataKeys(t.TriggerData, "trigger");
            }
        }

        private static void ValidateRange(string? value, int from, int to, string name, string parent)
        {
            ValidateMinLength(value, from, name, parent);
            ValidateMaxLength(value, to, name, parent);
        }

        private static void ValidateRangeValue(int? value, int from, int to, string name, string parent)
        {
            ValidateMinValue(value, from, name, parent);
            ValidateMaxValue(value, to, name, parent);
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
                if (t.RepeatCount > 0 && t.Interval.TotalSeconds < 60) { throw new RestValidationException("interval", $"interval has invalid value. interval must be greater or equals to 1 minute"); }
            });
        }

        private static void ValidateTriggerMisfireBehaviour(ITriggersContainer container)
        {
            var simpleValues = new[] { "firenow", "ignoremisfires", "nextwithexistingcount", "nextwithremainingcount", "nowwithexistingcount", "nowwithremainingcount" };
            container.SimpleTriggers?.ForEach(t =>
            {
                if (t.MisfireBehaviour.HasValue() && simpleValues.NotContains(t.MisfireBehaviour?.ToLower()))
                {
                    throw new RestValidationException("misfireBehaviour", $"value {t.MisfireBehaviour} is not valid value for simple trigger misfire behaviour");
                }
            });

            var cronValues = new[] { "donothing", "fireandproceed", "ignoremisfires" };
            container.CronTriggers?.ForEach(t =>
            {
                if (t.MisfireBehaviour.HasValue() && cronValues.NotContains(t.MisfireBehaviour?.ToLower()))
                {
                    throw new RestValidationException("misfireBehaviour", $"value {t.MisfireBehaviour} is not valid value for cron trigger misfire behaviour");
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
                if (t.Priority < 0 || t.Priority > 100) { throw new RestValidationException("priority", $"priority has invalid value ({t.Priority}). valid scope of values is 0-100"); }
            }
        }

        private static void ValidateTriggerRepeatCount(ITriggersContainer container)
        {
            container.SimpleTriggers?.ForEach(t =>
            {
                if (t.RepeatCount < 0) { throw new RestValidationException("repeat count", $"repeat count has invalid value. repeat count must be greater to 1"); }
            });
        }

        private static void ValidateTriggerRetry(TriggerPool pool)
        {
            foreach (var t in pool.Triggers)
            {
                if ((t.RetrySpan == null || t.RetrySpan == TimeSpan.Zero) && t.MaxRetries > 0) { throw new RestValidationException("retry span", $"retry span has invalid value. retry span must have value when max retries has value"); }
            }
        }

        private static void ValidateTriggerStartEnd(ITriggersContainer container)
        {
            container.SimpleTriggers?.ForEach(t =>
            {
                if (t.Start.HasValue && t.End.HasValue && t.Start.Value >= t.End.Value)
                {
                    throw new RestValidationException("end", $"end time has invalid value. end time cannot be before start time");
                }

                if (t.End.HasValue && t.End.Value <= DateTime.Now)
                {
                    throw new RestValidationException("end", $"end time has invalid value. end time cannot be before current server time");
                }
            });
        }

        private static void ValidateTriggerTimeout(TriggerPool pool)
        {
            foreach (var t in pool.Triggers)
            {
                if (t.Timeout.HasValue && t.Timeout.Value.TotalSeconds < 1) { throw new RestValidationException("timeout", $"timeout has invalid value. timeout must be greater or equals to 1 second"); }
            }
        }

        private async Task<JobIdResponse> Add(SetJobDynamicRequest request)
        {
            // Validation
            ValidateRequestNoNull(request);
            await ValidateRequestProperties(request);
            var jobKey = ValidateJobMetadata(request, Scheduler);
            await ValidateJobNotExists(jobKey);

            // Create Job (JobType+Concurrent, JobGroup, JobName, Description, Durable)
            var job = BuildJobDetails(request, jobKey);

            // Add Author, RetentionDays
            AddAuthor(request, job);
            AddLogRetentionDays(request, job);

            // Build Data
            BuildJobData(request, job);

            // Create Job Id
            var id = CreateJobId(job);

            // Build Triggers
            var triggers = BuildTriggers(request, id);

            // Save Job Properties
            var jobPropertiesYml = GetJopPropertiesYml(request);
            await DataLayer.AddJobProperty(new JobProperty { JobId = id, Properties = jobPropertiesYml });

            try
            {
                // ScheduleJob
                await Scheduler.ScheduleJob(job, triggers, true);
            }
            catch
            {
                // roll back
                await DataLayer.DeleteJobProperty(id);
                throw;
            }

            AuditJobSafe(jobKey, "job added", request);

            // Return Id
            return new JobIdResponse { Id = id };
        }

        private async Task<string> GetJobFileContent(SetJobPathRequest request)
        {
            await ValidateAddPath(request);
            string yml;
            var filename = GetJobFileFullName(request);
            try
            {
                yml = File.ReadAllText(filename);
            }
            catch (Exception ex)
            {
                throw new RestGeneralException($"fail to read file: {filename}", ex);
            }

            return yml;
        }

        private async Task ValidateAddPath(SetJobPathRequest request)
        {
            ValidateRequestNoNull(request);
            if (string.IsNullOrEmpty(request.JobFileName)) { request.JobFileName = FolderConsts.JobFileName; }

            try
            {
                ServiceUtil.ValidateJobFolderExists(request.Folder);
                var util = _serviceProvider.GetRequiredService<ClusterUtil>();
                await util.ValidateJobFolderExists(request.Folder);
            }
            catch (PlanarException ex)
            {
                throw new RestValidationException("folder", ex.Message);
            }

            try
            {
                ServiceUtil.ValidateJobFileExists(request.Folder, request.JobFileName);
                var util = _serviceProvider.GetRequiredService<ClusterUtil>();
                await util.ValidateJobFileExists(request.Folder, request.JobFileName);
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
        {
            if (yml == null)
            {
                throw new RestValidationException("properties", "properties is null or empty");
            }

            var properties = YmlUtil.Deserialize<TProperties>(yml) ??
                throw new RestValidationException("properties", "properties is null or empty");

            var validator = _serviceProvider.GetService<IValidator<TProperties>>();

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

                default:
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
}
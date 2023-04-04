using CommonJob;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public partial class JobDomain
    {
        private const int MinNameLength = 3;
        private const int MaxNameLength = 50;
        private const string NameRegexTemplate = @"^[a-zA-Z0-9\-_\s]{@MinNameLength@,@MaxNameLength@}$";

        private static readonly Regex _regex = new(
            NameRegexTemplate
                .Replace("@MinNameLength@", MinNameLength.ToString())
                .Replace("@MaxNameLength@", MaxNameLength.ToString()), RegexOptions.Compiled, TimeSpan.FromSeconds(5));

        public async Task<JobIdResponse> Add<TProperties>(SetJobRequest<TProperties> genericRequest)
           where TProperties : class, new()
        {
            ValidateRequestNoNull(genericRequest);
            var request = Mapper.Map<SetJobRequest<TProperties>, SetJobDynamicRequest>(genericRequest);
            return await Add(request);
        }

        public async Task<JobIdResponse> AddByPath(SetJobPathRequest request)
        {
            await ValidateAddPath(request);
            var yml = await GetJobFileContent(request);
            var dynamicRequest = GetJobDynamicRequest(yml, request);
            var response = await Add(dynamicRequest);
            return response;
        }

        private async Task<JobIdResponse> Add(SetJobDynamicRequest request)
        {
            // Validation
            ValidateRequestNoNull(request);
            await ValidateRequestProperties(request);
            var jobKey = ValidateJobMetadata(request);
            await ValidateJobNotExists(jobKey);

            // Global Config
            var config = ConvertToGlobalConfig(request.GlobalConfig);
            await ValidateGlobalConfig(config);
            await PutGlobalConfig(config);

            // Create Job (JobType+Concurent, JobGroup, JobName, Description, Durable)
            var job = BuildJobDetails(request, jobKey);

            // Add Author
            AddAuthor(request, job);

            // Build Data
            BuildJobData(request, job);

            // Create Job Id
            var id = CreateJobId(job);

            // Build Triggers
            var triggers = BuildTriggers(request);

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

            // Return Id
            return new JobIdResponse { Id = id };
        }

        // JobType+Concurent, JobGroup, JobName, Description, Durable
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

        // JobType+Concurent, JobGroup, JobName, Description, Durable
        private static IJobDetail CloneJobDetails(IJobDetail source)
        {
            var jobBuilder = JobBuilder.Create(source.JobType)
                .WithIdentity(source.Key)
                .WithDescription(source.Description)
                .RequestRecovery();

            if (source.Durable)
            {
                jobBuilder = jobBuilder.StoreDurably(true);
            }

            var job = jobBuilder.Build();
            return job;
        }

        private static void ValidateRequestNoNull(object request)
        {
            if (request == null)
            {
                throw new RestValidationException("request", "request is null");
            }
        }

        private static SetJobDynamicRequest GetJobDynamicRequest(string yml, SetJobPathRequest request)
        {
            SetJobDynamicRequest dynamicRequest;

            try
            {
                dynamicRequest = YmlUtil.Deserialize<SetJobDynamicRequest>(yml);
            }
            catch (Exception ex)
            {
                var filename = GetJobFileFullName(request);
                throw new RestGeneralException($"fail to deserialize file: {filename}", ex);
            }

            return dynamicRequest;
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

        private static string GetJobFileFullName(SetJobPathRequest request)
        {
            var filename = ServiceUtil.GetJobFilename(request.Folder, request.JobFileName);
            return filename;
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

        private static IReadOnlyCollection<ITrigger> BuildTriggers(SetJobRequest job)
        {
            var quartzTriggers1 = BuildTriggerWithSimpleSchedule(job.SimpleTriggers);
            var quartzTriggers2 = BuildTriggerWithCronSchedule(job.CronTriggers);
            var allTriggers = new List<ITrigger>();
            if (quartzTriggers1 != null) { allTriggers.AddRange(quartzTriggers1); }
            if (quartzTriggers2 != null) { allTriggers.AddRange(quartzTriggers2); }
            return allTriggers;
        }

        public static IEnumerable<ITrigger> BuildTriggerWithCronSchedule(List<JobCronTriggerMetadata> triggers)
        {
            if (triggers.IsNullOrEmpty()) { return new List<ITrigger>(); }

            var result = triggers.Select(t =>
            {
                var trigger = GetBaseTriggerBuilder(t)
                    .WithCronSchedule(t.CronExpression, c => BuidCronSchedule(c, t));

                return trigger.Build();
            });

            return result;
        }

        public static IEnumerable<ITrigger> BuildTriggerWithSimpleSchedule(List<JobSimpleTriggerMetadata> triggers)
        {
            if (triggers.IsNullOrEmpty()) { return new List<ITrigger>(); }

            var result = triggers.Select(t =>
            {
                var trigger = GetBaseTriggerBuilder(t);

                if (t.Start == null)
                {
                    trigger = trigger.StartAt(new DateTimeOffset(DateTime.Now.AddSeconds(3)));
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

        private static TriggerBuilder GetBaseTriggerBuilder(BaseTrigger jobTrigger)
        {
            var id =
                string.IsNullOrEmpty(jobTrigger.Id) ?
                ServiceUtil.GenerateId() :
                jobTrigger.Id;

            var trigger = TriggerBuilder.Create();

            if (string.IsNullOrEmpty(jobTrigger.Group))
            {
                trigger = trigger.WithIdentity(jobTrigger.Name ?? string.Empty);
            }
            else
            {
                trigger = trigger.WithIdentity(jobTrigger.Name ?? string.Empty, jobTrigger.Group);
            }

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

            // Data --> Retry span
            if (jobTrigger.RetrySpan.HasValue)
            {
                trigger = trigger.UsingJobData(Consts.RetrySpan, jobTrigger.RetrySpan.Value.ToSimpleTimeString());
            }

            return trigger;
        }

        private static void BuildJobData(SetJobRequest metadata, IJobDetail job)
        {
            if (metadata.JobData != null)
            {
                foreach (var item in metadata.JobData)
                {
                    job.JobDataMap[item.Key] = item.Value;
                }
            }
        }

        private static void AddAuthor(SetJobRequest metadata, IJobDetail job)
        {
            if (!string.IsNullOrEmpty(metadata.Author))
            {
                job.JobDataMap[Consts.Author] = metadata.Author;
            }
        }

        private static string CreateJobId(IJobDetail job)
        {
            // job id
            var id = ServiceUtil.GenerateId();
            job.JobDataMap.Add(Consts.JobId, id);

            return id;
        }

        private static IEnumerable<GlobalConfig> ConvertToGlobalConfig(Dictionary<string, string?> config)
        {
            if (config == null) { return Array.Empty<GlobalConfig>(); }
            var result = config.Select(c => new GlobalConfig { Key = c.Key, Value = c.Value, Type = "string" });
            return result;
        }

        private static async Task ValidateGlobalConfig(IEnumerable<GlobalConfig> config)
        {
            foreach (var p in config)
            {
                var validator = new GlobalConfigDataValidator();
                await validator.ValidateAndThrowAsync(p);
            }
        }

        private async Task PutGlobalConfig(IEnumerable<GlobalConfig> config)
        {
            var configDomain = Resolve<ConfigDomain>();
            foreach (var p in config)
            {
                await configDomain.Put(p);
            }
        }

        private static JobKey ValidateJobMetadata(SetJobRequest metadata)
        {
            metadata.JobData ??= new Dictionary<string, string?>();

            #region Trim

            metadata.Name = metadata.Name.SafeTrim();
            metadata.Group = metadata.Group.SafeTrim();
            metadata.Description = metadata.Description.SafeTrim();
            metadata.JobType = metadata.JobType.SafeTrim();

            #endregion Trim

            #region Mandatory

            if (string.IsNullOrEmpty(metadata.Name)) throw new RestValidationException("name", "job name is mandatory");
            if (string.IsNullOrEmpty(metadata.JobType)) throw new RestValidationException("type", "job type is mandatory");

            foreach (var item in metadata.JobData)
            {
                if (string.IsNullOrEmpty(item.Key)) throw new RestValidationException("key", "job data key must have value");
            }

            #endregion Mandatory

            #region JobType

            if (!BaseCommonJob.JobTypes.Contains(metadata.JobType))
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

            #region Max Chars

            ValidateRange(metadata.Name, 5, 50, "name", "job");
            ValidateRange(metadata.Group, 1, 50, "group", "job");
            ValidateMaxLength(metadata.Author, 200, "author", "job");
            ValidateMaxLength(metadata.Description, 100, "description", "job");

            foreach (var item in metadata.JobData)
            {
                ValidateRange(item.Key, 1, 100, "key", "job data");
                ValidateMaxLength(item.Value, 1000, "value", "job data");
            }

            #endregion Max Chars

            #region JobData

            if (metadata.JobData != null && metadata.JobData.Any() && metadata.Concurrent)
            {
                throw new RestValidationException("concurrent", $"job with concurrent=true can not have data. persist data with concurent running may cause unexpected results");
            }

            if (metadata.JobData != null)
            {
                foreach (var item in metadata.JobData)
                {
                    if (!Consts.IsDataKeyValid(item.Key)) throw new RestValidationException("key", $"job data key '{item.Key}' is invalid");
                }
            }

            #endregion JobData

            #region GlobalConfig

            foreach (var item in metadata.GlobalConfig)
            {
                ValidateRange(item.Key, 1, 100, "key", "job global config");
                ValidateMaxLength(item.Value, 1000, "value", "job global config");
            }

            #endregion GlobalConfig

            var triggersCount = metadata.CronTriggers?.Count + metadata.SimpleTriggers?.Count;
            if (triggersCount == 0 && metadata.Durable == false)
            {
                throw new RestValidationException("durable", $"job without any trigger must be durable. set the durable property to true or add at least one trigger");
            }

            ValidateTriggerMetadata(metadata);

            var jobKey = JobKeyHelper.GetJobKey(metadata);
            return jobKey;
        }

        private static bool IsRegexMatch(Regex regex, string? value)
        {
            if (value == null) { return true; }
            return regex.IsMatch(value);
        }

        private async Task ValidateJobNotExists(JobKey jobKey)
        {
            var exists = await Scheduler.GetJobDetail(jobKey);

            if (exists != null)
            {
                throw new RestConflictException($"job with name: {jobKey.Name} and group: {jobKey.Group} already exists");
            }
        }

        public static void ValidateTriggerMetadata(ITriggersContainer container)
        {
            TrimTriggerProperties(container);
            ValidateMandatoryTriggerProperties(container);
            ValidateTriggerNameProperties(container);
            ValidateMaxCharsTiggerProperties(container);
            ValidatePreserveWordsTriggerProperties(container);
            ValidateTriggerPriority(container);
            ValidateCronExpression(container);
            ValidateTriggerMisfireBehaviour(container);
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

        private static void ValidateTriggerPriority(ITriggersContainer container)
        {
            container.SimpleTriggers?.ForEach(t =>
            {
                if (t.Priority < 0 || t.Priority > 100) { throw new RestValidationException("priority", $"priority has invalid value ({t.Priority}). valid scope of values is 0-100"); }
            });
            container.CronTriggers?.ForEach(t =>
            {
                if (t.Priority < 0 || t.Priority > 100) { throw new RestValidationException("priority", $"priority has invalid value ({t.Priority}). valid scope of values is 0-100"); }
            });
        }

        private static void ValidateCronExpression(ITriggersContainer container)
        {
            container.CronTriggers?.ForEach(t =>
            {
                if (string.IsNullOrEmpty(t.CronExpression)) { throw new RestValidationException("priority", "cron expression is mandatory in cron trigger"); }
            });
        }

        private static void ValidatePreserveWordsTriggerProperties(ITriggersContainer container)
        {
            container.SimpleTriggers?.ForEach(t =>
            {
                t.TriggerData ??= new Dictionary<string, string?>();
                if (Consts.PreserveGroupNames.Contains(t.Group)) { throw new RestValidationException("group", $"simple trigger group '{t.Group}' is invalid (preserved value)"); }
                if (t.Name != null && t.Name.StartsWith(Consts.RetryTriggerNamePrefix)) { throw new RestValidationException("name", $"simple trigger name '{t.Name}' has invalid prefix"); }
                foreach (var item in t.TriggerData)
                {
                    if (!Consts.IsDataKeyValid(item.Key)) throw new RestValidationException("key", $"trigger data key '{item.Key}' is invalid");
                }
            });
            container.CronTriggers?.ForEach(t =>
            {
                t.TriggerData ??= new Dictionary<string, string?>();
                if (Consts.PreserveGroupNames.Contains(t.Group)) { throw new RestValidationException("group", $"cron trigger group '{t.Group}' is invalid (preserved value)"); }
                if (t.Name != null && t.Name.StartsWith(Consts.RetryTriggerNamePrefix)) { throw new RestValidationException("name", $"cron trigger name '{t.Name}' has invalid prefix"); }
                foreach (var item in t.TriggerData)
                {
                    if (!Consts.IsDataKeyValid(item.Key)) throw new RestValidationException("key", $"trigger data key '{item.Key}' is invalid");
                }
            });
        }

        private static void ValidateMaxCharsTiggerProperties(ITriggersContainer container)
        {
            container.SimpleTriggers?.ForEach(t =>
            {
                t.TriggerData ??= new Dictionary<string, string?>();
                ValidateRange(t.Name, 5, 50, "name", "trigger");
                ValidateRange(t.Group, 1, 50, "group", "trigger");

                foreach (var item in t.TriggerData)
                {
                    ValidateRange(item.Key, 1, 100, "key", "trigger data");
                    ValidateMaxLength(item.Value, 1000, "value", "trigger data");
                }
            });

            container.CronTriggers?.ForEach(t =>
            {
                t.TriggerData ??= new Dictionary<string, string?>();
                ValidateRange(t.Name, 5, 50, "name", "trigger");
                ValidateRange(t.Group, 1, 50, "group", "trigger");

                foreach (var item in t.TriggerData)
                {
                    ValidateRange(item.Key, 1, 100, "key", "trigger data");
                    ValidateMaxLength(item.Value, 1000, "value", "trigger data");
                }
            });
        }

        private static void ValidateTriggerNameProperties(ITriggersContainer container)
        {
            container.SimpleTriggers?.ForEach(t =>
            {
                if (!IsRegexMatch(_regex, t.Name)) throw new RestValidationException("name", $"trigger name '{t.Name}' is invalid. use only alphanumeric, dashes & underscore");
                if (!IsRegexMatch(_regex, t.Group)) throw new RestValidationException("group", $"trigger group '{t.Group}' is invalid. use only alphanumeric, dashes & underscore");
            });
            container.CronTriggers?.ForEach(t =>
            {
                if (!IsRegexMatch(_regex, t.Name)) throw new RestValidationException("name", $"trigger name '{t.Name}' is invalid. use only alphanumeric, dashes & underscore");
                if (!IsRegexMatch(_regex, t.Group)) throw new RestValidationException("group", $"trigger group '{t.Group}' is invalid. use only alphanumeric, dashes & underscore");
            });
        }

        private static void ValidateMandatoryTriggerProperties(ITriggersContainer container)
        {
            container.SimpleTriggers?.ForEach(t =>
            {
                t.TriggerData ??= new Dictionary<string, string?>();
                if (string.IsNullOrEmpty(t.Name)) throw new RestValidationException("name", "trigger name is mandatory");
                foreach (var item in t.TriggerData)
                {
                    if (string.IsNullOrEmpty(item.Key)) throw new RestValidationException("key", "trigger data key must have value");
                }
            });
            container.CronTriggers?.ForEach(t =>
            {
                t.TriggerData ??= new Dictionary<string, string?>();
                if (string.IsNullOrEmpty(t.Name)) throw new RestValidationException("name", "trigger name is mandatory");
            });
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
                typeName = $"Planar.{job.JobType}Concurent";
            }
            else
            {
                typeName = $"Planar.{job.JobType}NoConcurent";
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

                default:
                    break;
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

        private static void ValidateRange(string? value, int from, int to, string name, string parent)
        {
            ValidateMinLength(value, from, name, parent);
            ValidateMaxLength(value, to, name, parent);
        }

        private static void ValidateMaxLength(string? value, int length, string name, string parent)
        {
            if (value != null && value.Length > length)
            {
                throw new RestValidationException(name, $"{parent} {name} length is invalid. maximum length is {length}");
            }
        }

        private static void ValidateMinLength(string? value, int length, string name, string parent)
        {
            if (value == null) { return; }
            if (value.Length < length) throw new RestValidationException(name, $"{parent} {name} length is invalid. minimum length is {length}");
        }
    }
}
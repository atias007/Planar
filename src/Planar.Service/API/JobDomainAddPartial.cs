using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
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
        private const string NameRegexTemplate = @"^[a-zA-Z0-9\-_]{@MinNameLength@,@MaxNameLength@}$";

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

        public async Task<JobIdResponse> AddByFolder(SetJobFoldeRequest request)
        {
            await ValidateAddFolder(request);
            var yml = await GetJobFileContent(request);
            var dynamicRequest = GetJobDynamicRequest(yml, request.Folder);
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
            await UpsertGlobalConfig(config);

            // Create Job
            var job = BuildJobDetails(request, jobKey);

            // Build Data
            BuildJobData(request, job);

            // Create Job Id
            var id = CreateJobId(job);

            // Build Triggers
            var triggers = BuildTriggers(request);

            // Save Job Properties
            var jobPropertiesYml = GetJopPropertiesYml(request);
            await DataLayer.AddJobProperty(new JobProperty { JobId = id, Properties = jobPropertiesYml });

            // ScheduleJob
            await Scheduler.ScheduleJob(job, triggers, true);

            // Return Id
            return new JobIdResponse { Id = id };
        }

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

        private static void ValidateRequestNoNull(object request)
        {
            if (request == null)
            {
                throw new RestValidationException("request", "request is null");
            }
        }

        private static SetJobDynamicRequest GetJobDynamicRequest(string yml, string folder)
        {
            SetJobDynamicRequest dynamicRequest;

            try
            {
                dynamicRequest = YmlUtil.Deserialize<SetJobDynamicRequest>(yml);
            }
            catch (Exception ex)
            {
                var filename = GetJobFileFullName(folder);
                throw new RestGeneralException($"Fail to deserialize file: {filename}", ex);
            }

            return dynamicRequest;
        }

        private async Task<string> GetJobFileContent(SetJobFoldeRequest request)
        {
            await ValidateAddFolder(request);
            string yml;
            var filename = GetJobFileFullName(request.Folder);
            try
            {
                yml = File.ReadAllText(filename);
            }
            catch (Exception ex)
            {
                throw new RestGeneralException($"Fail to read file: {filename}", ex);
            }

            return yml;
        }

        private static string GetJobFileFullName(string folder)
        {
            var filename = ServiceUtil.GetJobFilename(folder, FolderConsts.JobFileName);
            return filename;
        }

        private static string GetJopPropertiesYml(SetJobDynamicRequest request)
        {
            if (request.Properties == null)
            {
                return null;
            }

            var yml = YmlUtil.Serialize(request.Properties);
            return yml;
        }

        private async Task ValidateAddFolder(SetJobFoldeRequest request)
        {
            ValidateRequestNoNull(request);

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
                ServiceUtil.ValidateJobFileExists(request.Folder, FolderConsts.JobFileName);
                var util = _serviceProvider.GetRequiredService<ClusterUtil>();
                await util.ValidateJobFileExists(request.Folder, FolderConsts.JobFileName);
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
                trigger = trigger.WithIdentity(jobTrigger.Name);
            }
            else
            {
                trigger = trigger.WithIdentity(jobTrigger.Name, jobTrigger.Group);
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
            jobTrigger.TriggerData ??= new Dictionary<string, string>();

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
            // job data
            if (metadata.JobData != null)
            {
                foreach (var item in metadata.JobData)
                {
                    job.JobDataMap[item.Key] = item.Value;
                }
            }
        }

        private static string CreateJobId(IJobDetail job)
        {
            // job id
            var id = ServiceUtil.GenerateId();
            job.JobDataMap.Add(Consts.JobId, id);

            return id;
        }

        private static IEnumerable<GlobalConfig> ConvertToGlobalConfig(Dictionary<string, string> config)
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

        private async Task UpsertGlobalConfig(IEnumerable<GlobalConfig> config)
        {
            var configDomain = Resolve<ConfigDomain>();
            foreach (var p in config)
            {
                await configDomain.Upsert(p);
            };
        }

        private static JobKey ValidateJobMetadata(SetJobRequest metadata)
        {
            #region Trim

            metadata.Name = metadata.Name.SafeTrim();
            metadata.Group = metadata.Group.SafeTrim();
            metadata.Description = metadata.Description.SafeTrim();
            metadata.JobType = metadata.JobType.SafeTrim();

            #endregion Trim

            #region Mandatory

            if (string.IsNullOrEmpty(metadata.Name)) throw new RestValidationException("name", "job name is mandatory");
            if (string.IsNullOrEmpty(metadata.JobType)) throw new RestValidationException("type", "job type is mandatory");

            #endregion Mandatory

            #region Valid Name & Group

            if (!IsRegexMatch(_regex, metadata.Name))
            {
                throw new RestValidationException("name", $"job name '{metadata.Name}' is invalid. use only alphanumeric, dashes & underscore");
            }

            if (!IsRegexMatch(_regex, metadata.Group))
            {
                throw new RestValidationException("group", $"job group '{metadata.Group}' is invalid. use only alphanumeric, dashes & underscore");
            }

            #endregion Valid Name & Group

            #region Max Chars

            if (metadata.Name.Length > 50) throw new RestValidationException("name", "job name length is invalid. max length is 50");
            if (metadata.Group?.Length > 50) throw new RestValidationException("group", "job group length is invalid. max length is 50");
            if (metadata.Description?.Length > 100) throw new RestValidationException("description", "job description length is invalid. max length is 100");

            #endregion Max Chars

            if (Consts.PreserveGroupNames.Contains(metadata.Group))
            {
                throw new RestValidationException("group", $"job group '{metadata.Group}' is invalid (preserved value)");
            }

            if (metadata.JobData != null && metadata.JobData.Any() && metadata.Concurrent)
            {
                throw new RestValidationException("concurrent", $"job with concurrent=true can not have data. persist data with concurent running may cause unexpected results");
            }

            var triggersCount = metadata.CronTriggers?.Count + metadata.SimpleTriggers?.Count;
            if (triggersCount == 0 && metadata.Durable == false)
            {
                throw new RestValidationException("durable", $"job without any trigger must be durable. set the durable property to true or add at least one trigger");
            }

            ValidateTriggerMetadata(metadata);

            var jobKey = JobKeyHelper.GetJobKey(metadata);
            return jobKey;
        }

        private static bool IsRegexMatch(Regex regex, string value)
        {
            if (value == null) return true;
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
            ValidateTriggerMisfireBehaviour(container);
        }

        private static void ValidateTriggerMisfireBehaviour(ITriggersContainer container)
        {
            var simpleValues = new[] { "firenow", "ignoremisfires", "nextwithexistingcount", "nextwithremainingcount", "nowwithexistingcount", "nowwithremainingcount" };
            container.SimpleTriggers?.ForEach(t =>
            {
                if (t.MisfireBehaviour.HasValue() && simpleValues.NotContains(t.MisfireBehaviour.ToLower()))
                {
                    throw new RestValidationException("misfireBehaviour", $"value {t.MisfireBehaviour} is not valid value for simple trigger misfire behaviour");
                }
            });

            var cronValues = new[] { "donothing", "fireandproceed", "ignoremisfires" };
            container.CronTriggers?.ForEach(t =>
            {
                if (t.MisfireBehaviour.HasValue() && cronValues.NotContains(t.MisfireBehaviour.ToLower()))
                {
                    throw new RestValidationException("misfireBehaviour", $"value {t.MisfireBehaviour} is not valid value for cron trigger misfire behaviour");
                }
            });
        }

        private static void ValidateTriggerPriority(ITriggersContainer container)
        {
            container.SimpleTriggers?.ForEach(t =>
            {
                if (t.Priority == int.MaxValue) { throw new RestValidationException("priority", $"priority ha invalid value ({t.Priority})"); }
            });
            container.CronTriggers?.ForEach(t =>
            {
                if (t.Priority == int.MaxValue) { throw new RestValidationException("priority", $"priority ha invalid value ({t.Priority})"); }
            });
        }

        private static void ValidatePreserveWordsTriggerProperties(ITriggersContainer container)
        {
            container.SimpleTriggers?.ForEach(t =>
            {
                if (Consts.PreserveGroupNames.Contains(t.Group)) { throw new RestValidationException("group", $"simple trigger group '{t.Group}' is invalid (preserved value)"); }
                if (t.Name.StartsWith(Consts.RetryTriggerNamePrefix)) { throw new RestValidationException("name", $"simple trigger name '{t.Name}' has invalid prefix"); }
            });
            container.CronTriggers?.ForEach(t =>
            {
                if (Consts.PreserveGroupNames.Contains(t.Group)) { throw new RestValidationException("group", $"cron trigger group '{t.Group}' is invalid (preserved value)"); }
                if (t.Name.StartsWith(Consts.RetryTriggerNamePrefix)) { throw new RestValidationException("name", $"cron trigger name '{t.Name}' has invalid prefix"); }
            });
        }

        private static void ValidateMaxCharsTiggerProperties(ITriggersContainer container)
        {
            container.SimpleTriggers?.ForEach(t =>
            {
                if (t.Name.Length > 50) throw new RestValidationException("name", "trigger name length is invalid. max length is 50");
                if (t.Group?.Length > 50) throw new RestValidationException("group", "trigger group length is invalid. max length is 50");
            });
            container.CronTriggers?.ForEach(t =>
            {
                if (t.Name.Length > 50) throw new RestValidationException("name", "trigger name length is invalid. max length is 50");
                if (t.Group?.Length > 50) throw new RestValidationException("group", "trigger group length is invalid. max length is 50");
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
                if (string.IsNullOrEmpty(t.Name)) throw new RestValidationException("name", "trigger name is mandatory");
            });
            container.CronTriggers?.ForEach(t =>
            {
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
            Assembly assembly;
            string typeName;

            switch (job.JobType)
            {
                case nameof(PlanarJob):
                    try
                    {
                        assembly = Assembly.Load(nameof(PlanarJob));

                        if (job.Concurrent)
                        {
                            typeName = $"Planar.{nameof(PlanarJobConcurent)}";
                        }
                        else
                        {
                            typeName = $"Planar.{nameof(PlanarJobNoConcurent)}";
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new RestValidationException("jobType", $"fail to load assemly {nameof(PlanarJob)} ({ex.Message})");
                    }
                    break;

                default:
                    throw new RestValidationException("jobType", $"job type '{job.JobType}' is not supported");
            }

            try
            {
                var type = assembly.GetType(typeName);
                if (type == null) throw new RestValidationException("jobType", $"type {typeName} is not supported");
                return type;
            }
            catch (Exception ex)
            {
                throw new RestValidationException("jobType", $"Fail to get type {job.JobType} from assemly {assembly.FullName} ({ex.Message})");
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
                    var properties = YmlUtil.Deserialize<PlanarJobProperties>(yml);
                    await ValidatePlanarJobProperties(properties);
                    break;

                default:
                    break;
            }
        }

        private async Task ValidatePlanarJobProperties<TProperties>(TProperties properties)
        {
            if (properties == null)
            {
                throw new RestValidationException("properties", "properties is null or empty");
            }

            var validator = _serviceProvider.GetService<IValidator<TProperties>>();

            if (validator == null)
            {
                Logger.LogWarning("Job properties of type {PropertyType} has no registered validation in DI. validation skipped", typeof(TProperties).FullName);
                return;
            }

            await validator.ValidateAndThrowAsync(properties);
        }
    }
}
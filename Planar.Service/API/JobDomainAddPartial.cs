using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.API.Helpers;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Planar.Service.Model;
using Planar.Service.Validation;
using Quartz;
using RunPlanarJob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Planar.Service.API
{
    public partial class JobDomain
    {
        private const string nameRegex = @"^[a-zA-Z0-9\-_]+$";

        private enum TriggerType
        {
            Simple,
            Cron
        }

        public async Task<JobIdResponse> Add(AddJobRequest request)
        {
            await ValidateAdd(request);

            var jobKey = await ValidateJobMetadata(request);
            var config = GetGlobalConfig(request.GlobalConfig);
            await ValidateGlobalConfig(config);
            await BuildGlobalConfig(config);

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

            var id = BuildJobData(request, job);
            await BuildTriggers(Scheduler, job, request);
            return new JobIdResponse { Id = id };
        }

        public async Task<JobIdResponse> AddFolder(AddJobFoldeRequest request)
        {
            await ValidateAddFolder(request);

            string yml;
            var filename = ServiceUtil.GetJobFilename(request.Folder, FolderConsts.JobFileName);
            try
            {
                yml = File.ReadAllText(filename);
            }
            catch (Exception ex)
            {
                throw new RestGeneralException($"Fail to read file: {filename}", ex);
            }

            AddJobRequest subrequest;
            try
            {
                var deserializer = new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .Build();

                subrequest = deserializer.Deserialize<AddJobRequest>(yml);
            }
            catch (Exception ex)
            {
                throw new RestGeneralException($"Fail to deserialize file: {filename}", ex);
            }

            if (subrequest.Properties.ContainsKey("JobPath", ignoreCase: true))
            {
                var value = subrequest.Properties.Get("JobPath", ignoreCase: true);
                if (string.IsNullOrEmpty(value) || value.Trim() == "." || value.Trim() == "./")
                {
                    subrequest.Properties.Set("JobPath", request.Folder, ignoreCase: true);
                }
            }

            var response = await Add(subrequest);
            return response;
        }

        private async Task ValidateAdd(AddJobRequest request)
        {
            if (request == null)
            {
                throw new RestValidationException("request", "request is null");
            }

            if (request.Properties.ContainsKey("JobPath", ignoreCase: true))
            {
                var path = request.Properties.Get("JobPath", ignoreCase: true);

                try
                {
                    ServiceUtil.ValidateJobFolderExists(path);
                    var util = _serviceProvider.GetRequiredService<ClusterUtil>();
                    await util.ValidateJobFolderExists(path);
                }
                catch (PlanarException ex)
                {
                    throw new RestValidationException("folder", ex.Message);
                }

                if (request.Properties.ContainsKey("Filename", ignoreCase: true))
                {
                    try
                    {
                        var filename = request.Properties.Get("FileName", ignoreCase: true);
                        ServiceUtil.ValidateJobFileExists(path, filename);
                        var util = _serviceProvider.GetRequiredService<ClusterUtil>();
                        await util.ValidateJobFileExists(path, filename);
                    }
                    catch (PlanarException ex)
                    {
                        throw new RestValidationException("filename", ex.Message);
                    }
                }
            }
        }

        private async Task ValidateAddFolder(AddJobFoldeRequest request)
        {
            if (request == null)
            {
                throw new RestValidationException("request", "request is null");
            }

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

        private static async Task BuildTriggers(IScheduler scheduler, IJobDetail quartzJob, AddJobRequest job)
        {
            var quartzTriggers1 = BuildTriggerWithSimpleSchedule(job.SimpleTriggers);
            var quartzTriggers2 = BuildTriggerWithCronSchedule(job.CronTriggers);
            var allTriggers = new List<ITrigger>();
            if (quartzTriggers1 != null) allTriggers.AddRange(quartzTriggers1);
            if (quartzTriggers2 != null) allTriggers.AddRange(quartzTriggers2);

            await scheduler.ScheduleJob(quartzJob, allTriggers, true);
        }

        public static IEnumerable<ITrigger> BuildTriggerWithCronSchedule(List<JobCronTriggerMetadata> triggers)
        {
            if (triggers.IsNullOrEmpty()) return null;

            var result = triggers.Select(t =>
            {
                var trigger = GetBaseTriggerBuilder(t, TriggerType.Cron)
                    .WithCronSchedule(t.CronExpression, c => BuidCronSchedule(c, t));

                return trigger.Build();
            });

            return result;
        }

        public static IEnumerable<ITrigger> BuildTriggerWithSimpleSchedule(List<JobSimpleTriggerMetadata> triggers)
        {
            if (triggers.IsNullOrEmpty()) return null;

            var result = triggers.Select(t =>
            {
                var trigger = GetBaseTriggerBuilder(t, TriggerType.Simple);

                if (t.Start == null)
                {
                    trigger = trigger.StartNow();
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

        private static TriggerBuilder GetBaseTriggerBuilder(BaseTrigger jobTrigger, TriggerType triggerType)
        {
            var id = ServiceUtil.GenerateId();
            var name = string.IsNullOrEmpty(jobTrigger.Name) ? $"{triggerType}Trigger_{id}" : jobTrigger.Name;
            var group = string.IsNullOrEmpty(jobTrigger.Group) ? null : jobTrigger.Group;

            var trigger = TriggerBuilder.Create();

            if (string.IsNullOrEmpty(group))
            {
                trigger = trigger.WithIdentity(name);
            }
            else
            {
                trigger = trigger.WithIdentity(name, group);
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
            if (jobTrigger.TriggerData == null)
            {
                jobTrigger.TriggerData = new Dictionary<string, string>();
            }

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

        private static string BuildJobData(AddJobRequest metadata, IJobDetail job)
        {
            // job data
            if (metadata.JobData != null)
            {
                foreach (var item in metadata.JobData)
                {
                    job.JobDataMap[item.Key] = item.Value;
                }
            }

            // properties
            metadata.Properties ??= new Dictionary<string, string>();
            var json = JsonConvert.SerializeObject(metadata.Properties);
            job.JobDataMap.Add(Consts.JobTypeProperties, json);

            // job id
            var id = ServiceUtil.GenerateId();
            job.JobDataMap.Add(Consts.JobId, id);

            return id;
        }

        private static IEnumerable<GlobalConfig> GetGlobalConfig(Dictionary<string, string> config)
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

        private async Task BuildGlobalConfig(IEnumerable<GlobalConfig> config)
        {
            var configDomain = Resolve<ConfigDomain>();
            foreach (var p in config)
            {
                await configDomain.Upsert(p);
            };
        }

        private static async Task<JobKey> ValidateJobMetadata(AddJobRequest metadata)
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

            var regex = new Regex(nameRegex);
            if (IsRegexMatch(regex, metadata.Name) == false) throw new RestValidationException("name", $"job name '{metadata.Name}' is invalid. use only alphanumeric, dashes & underscore");
            if (IsRegexMatch(regex, metadata.Group) == false) throw new RestValidationException("group", $"job group '{metadata.Group}' is invalid. use only alphanumeric, dashes & underscore");

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

            ValidateTriggerMetadata(metadata);

            var jobKey = JobKeyHelper.GetJobKey(metadata);
            await ValidateJobNotExists(jobKey);

            return jobKey;
        }

        private static bool IsRegexMatch(Regex regex, string value)
        {
            if (value == null) return true;
            return regex.IsMatch(value);
        }

        private static async Task ValidateJobNotExists(JobKey jobKey)
        {
            var exists = await Scheduler.GetJobDetail(jobKey);

            if (exists != null)
            {
                throw new RestValidationException("name,group", $"job with name: {jobKey.Name} and group: {jobKey.Group} already exists");
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
            var regex = new Regex(nameRegex);
            container.SimpleTriggers?.ForEach(t =>
            {
                if (IsRegexMatch(regex, t.Name) == false) throw new RestValidationException("name", $"trigger name '{t.Name}' is invalid. use only alphanumeric, dashes & underscore");
                if (IsRegexMatch(regex, t.Group) == false) throw new RestValidationException("group", $"trigger group '{t.Group}' is invalid. use only alphanumeric, dashes & underscore");
            });
            container.CronTriggers?.ForEach(t =>
            {
                if (IsRegexMatch(regex, t.Name) == false) throw new RestValidationException("name", $"trigger name '{t.Name}' is invalid. use only alphanumeric, dashes & underscore");
                if (IsRegexMatch(regex, t.Group) == false) throw new RestValidationException("group", $"trigger group '{t.Group}' is invalid. use only alphanumeric, dashes & underscore");
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

        private static Type GetJobType(AddJobRequest job)
        {
            Assembly assembly;
            string typeName;

            switch (job.JobType)
            {
                case nameof(PlanarJob):
                    try
                    {
                        assembly = Assembly.Load(nameof(RunPlanarJob));

                        if (job.Concurrent)
                        {
                            typeName = $"{nameof(RunPlanarJob)}.{nameof(PlanarJobConcurent)}";
                        }
                        else
                        {
                            typeName = $"{nameof(RunPlanarJob)}.{nameof(PlanarJobNoConcurent)}";
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new RestValidationException("jobType", $"fail to load assemly {nameof(RunPlanarJob)} ({ex.Message})");
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
    }
}
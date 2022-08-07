using Newtonsoft.Json;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.API.Helpers;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Quartz;
using RunPlanarJob;
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
        private const string nameRegex = @"^[a-zA-Z0-9\-_]+$";

        private enum TriggerType
        {
            Simple,
            Cron
        }

        public async Task<JobIdResponse> Add(AddJobRequest request)
        {
            if (request == null)
            {
                throw new RestValidationException("yaml", "fail to read yml data");
            }

            var jobKey = await ValidateJobMetadata(request);
            await BuildGlobalParameters(request.GlobalParameters);

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
            if (string.IsNullOrEmpty(jobTrigger.Calendar) == false)
            {
                trigger = trigger.ModifiedByCalendar(jobTrigger.Calendar);
            }

            // Data
            if (jobTrigger.TriggerData == null) { jobTrigger.TriggerData = new Dictionary<string, string>(); }
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
            if (metadata.Properties == null) { metadata.Properties = new Dictionary<string, string>(); }
            var json = JsonConvert.SerializeObject(metadata.Properties);
            job.JobDataMap.Add(Consts.JobTypeProperties, json);

            // job id
            var id = ServiceUtil.GenerateId();
            job.JobDataMap.Add(Consts.JobId, id);

            return id;
        }

        private async Task BuildGlobalParameters(Dictionary<string, string> parameters)
        {
            if (parameters != null)
            {
                var parametersDomain = Resolve<ParametersDomain>();
                foreach (var p in parameters)
                {
                    await parametersDomain.Upsert(new GlobalParameterData { Key = p.Key, Value = p.Value });
                };
            }
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

            if (Consts.PreserveGroupNames.Contains(metadata.Group)) { throw new RestValidationException("group", $"job group group '{metadata.Group}' is invalid (preserved value)"); }

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
                if (string.IsNullOrEmpty(t.MisfireBehaviour) == false)
                {
                    if (simpleValues.Contains(t.MisfireBehaviour.ToLower()) == false)
                    {
                        throw new RestValidationException("misfireBehaviour", $"value {t.MisfireBehaviour} is not valid value for simple trigger misfire behaviour");
                    }
                }
            });

            var cronValues = new[] { "donothing", "fireandproceed", "ignoremisfires" };
            container.SimpleTriggers?.ForEach(t =>
            {
                if (string.IsNullOrEmpty(t.MisfireBehaviour) == false)
                {
                    if (cronValues.Contains(t.MisfireBehaviour.ToLower()) == false)
                    {
                        throw new RestValidationException("misfireBehaviour", $"value {t.MisfireBehaviour} is not valid value for cron trigger misfire behaviour");
                    }
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
            });
            container.CronTriggers?.ForEach(t =>
            {
                if (Consts.PreserveGroupNames.Contains(t.Group)) { throw new RestValidationException("group", $"cron trigger group '{t.Group}' is invalid (preserved value)"); }
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
                            typeName = $"{nameof(RunPlanarJob)}.{nameof(PlanarJobConcurrent)}";
                        }
                        else
                        {
                            typeName = $"{nameof(RunPlanarJob)}.{nameof(PlanarJob)}";
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
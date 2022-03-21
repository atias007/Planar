using Newtonsoft.Json;
using Planner.API.Common.Entities;
using Planner.Common;
using Planner.Service.API.Helpers;
using Planner.Service.Exceptions;
using Planner.Service.General;
using Planner.Service.Model.Metadata;
using Quartz;
using RunPlannerJob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization.NamingConventions;
using System.IO.Compression;

namespace Planner.Service.API
{
    public partial class DeamonBL
    {
        private const string nameRegex = @"^[a-zA-Z0-9\-_]+$";

        private enum TriggerType
        {
            Simple,
            Cron
        }

        public async Task<AddJobResponse> AddJob(AddJobRequest request)
        {
            var metadata = GetJobMetadata(request.Yaml);
            AddPathRelativeFolder(metadata, request.Path);
            var jobKey = await ValidateJobMetadata(metadata);
            ExtractJobPackage(request.Path);
            await BuildGlobalParameters(metadata.GlobalParameters);

            var jobType = GetJobType(metadata);
            var jobBuilder = JobBuilder.Create(jobType)
                .WithIdentity(jobKey)
                .WithDescription(metadata.Description)
                .RequestRecovery();

            if (metadata == null || metadata.Durable.GetValueOrDefault())
            {
                jobBuilder = jobBuilder.StoreDurably(true);
            }

            var job = jobBuilder.Build();

            var id = BuildJobData(metadata, job);
            await BuildTriggers(Scheduler, job, metadata);
            return new AddJobResponse { Result = id };
        }

        private void ExtractJobPackage(string path)
        {
            try
            {
                var dirInfo = new DirectoryInfo(path);
                var files = dirInfo.GetFiles("*.nupkg").Concat(dirInfo.GetFiles("*.zip")).ToArray();

                if (files.Length == 0)
                {
                    _logger.LogInformation($"No nuget/zip packge found in directory '{dirInfo.FullName}' when add new job file");
                    return;
                }

                if (files.Length > 1)
                {
                    _logger.LogInformation($"More then 1 nuget/zip packge fount in directory '{dirInfo.FullName}' when add new job file. Found {files.Length} files");
                    return;
                }

                var package = files[0];
                _logger.LogInformation($"Extract nuget/zip package '{package.FullName}'");
                ZipFile.ExtractToDirectory(package.FullName, package.Directory.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Fail to extract nuget/zip package. Error: {ex.Message}", ex);
                throw;
            }
        }

        private static async Task BuildTriggers(IScheduler scheduler, IJobDetail quartzJob, JobMetadata job)
        {
            var quartzTriggers1 = BuildTriggerWithSimpleSchedule(job.SimpleTriggers);
            var quartzTriggers2 = BuildTriggerWithCronSchedule(job.CronTriggers);
            var allTriggers = new List<ITrigger>();
            if (quartzTriggers1 != null) allTriggers.AddRange(quartzTriggers1);
            if (quartzTriggers2 != null) allTriggers.AddRange(quartzTriggers2);

            await scheduler.ScheduleJob(quartzJob, allTriggers, true);
        }

        private static IEnumerable<ITrigger> BuildTriggerWithCronSchedule(List<JobCronTriggerMetadata> triggers)
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

        private static IEnumerable<ITrigger> BuildTriggerWithSimpleSchedule(List<JobSimpleTriggerMetadata> triggers)
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
            if (trigger.MisfireBehaviour.HasValue)
            {
                switch (trigger.MisfireBehaviour.Value)
                {
                    case 0:
                        builder.WithMisfireHandlingInstructionDoNothing();
                        break;

                    case 2:
                        builder.WithMisfireHandlingInstructionFireAndProceed();
                        break;

                    case 3:
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

            if (trigger.MisfireBehaviour.HasValue)
            {
                switch (trigger.MisfireBehaviour.Value)
                {
                    case 0:
                        builder.WithMisfireHandlingInstructionFireNow();
                        break;

                    case 1:
                        builder.WithMisfireHandlingInstructionIgnoreMisfires();
                        break;

                    case 2:
                        builder.WithMisfireHandlingInstructionNextWithExistingCount();
                        break;

                    case 3:
                        builder.WithMisfireHandlingInstructionNextWithRemainingCount();
                        break;

                    case 4:
                        builder.WithMisfireHandlingInstructionNowWithExistingCount();
                        break;

                    case 5:
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
            if (jobTrigger.TriggerData?.Count > 0)
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

        private static string BuildJobData(JobMetadata metadata, IJobDetail job)
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
                foreach (var p in parameters)
                {
                    await UpsertGlobalParameter(new GlobalParameterData { Key = p.Key, Value = p.Value });
                };
            }
        }

        private static async Task<JobKey> ValidateJobMetadata(JobMetadata metadata)
        {
            #region Trim

            metadata.Name = metadata.Name.SafeTrim();
            metadata.Group = metadata.Group.SafeTrim();
            metadata.Description = metadata.Description.SafeTrim();
            metadata.JobType = metadata.JobType.SafeTrim();

            #endregion Trim

            #region Mandatory

            if (string.IsNullOrEmpty(metadata.Name)) throw new PlannerValidationException("job name is mandatory");
            if (string.IsNullOrEmpty(metadata.JobType)) throw new PlannerValidationException("job type is mandatory");
            metadata.SimpleTriggers?.ForEach(t =>
            {
                if (string.IsNullOrEmpty(t.Name)) throw new PlannerValidationException("trigger name is mandatory");
            });
            metadata.CronTriggers?.ForEach(t =>
            {
                if (string.IsNullOrEmpty(t.Name)) throw new PlannerValidationException("trigger name is mandatory");
            });

            #endregion Mandatory

            #region Valid Name & Group

            var regex = new Regex(nameRegex);
            if (IsRegexMatch(regex, metadata.Name) == false) throw new PlannerValidationException($"job name '{metadata.Name}' is invalid. use only alphanumeric, dashes & underscore");
            if (IsRegexMatch(regex, metadata.Group) == false) throw new PlannerValidationException($"job group '{metadata.Group}' is invalid. use only alphanumeric, dashes & underscore");
            metadata.SimpleTriggers?.ForEach(t =>
            {
                if (IsRegexMatch(regex, t.Name) == false) throw new PlannerValidationException($"trigger name '{t.Name}' is invalid. use only alphanumeric, dashes & underscore");
                if (IsRegexMatch(regex, t.Group) == false) throw new PlannerValidationException($"trigger group '{t.Group}' is invalid. use only alphanumeric, dashes & underscore");
            });
            metadata.CronTriggers?.ForEach(t =>
            {
                if (IsRegexMatch(regex, t.Name) == false) throw new PlannerValidationException($"trigger name '{t.Name}' is invalid. use only alphanumeric, dashes & underscore");
                if (IsRegexMatch(regex, t.Group) == false) throw new PlannerValidationException($"trigger group '{t.Group}' is invalid. use only alphanumeric, dashes & underscore");
            });

            #endregion Valid Name & Group

            #region Max Chars

            if (metadata.Name.Length > 50) throw new PlannerValidationException("job name length is invalid. max length is 50");
            if (metadata.Group?.Length > 50) throw new PlannerValidationException("job group length is invalid. max length is 50");
            if (metadata.Description?.Length > 100) throw new PlannerValidationException("job description length is invalid. max length is 100");
            metadata.SimpleTriggers?.ForEach(t =>
            {
                if (t.Name.Length > 50) throw new PlannerValidationException("trigger name length is invalid. max length is 50");
                if (t.Group?.Length > 50) throw new PlannerValidationException("trigger group length is invalid. max length is 50");
            });
            metadata.CronTriggers?.ForEach(t =>
            {
                if (t.Name.Length > 50) throw new PlannerValidationException("trigger name length is invalid. max length is 50");
                if (t.Group?.Length > 50) throw new PlannerValidationException("trigger group length is invalid. max length is 50");
            });

            #endregion Max Chars

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

        private static void ValidateTriggerMetadata(JobMetadata metadata)
        {
            #region Trim

            metadata.SimpleTriggers?.ForEach(t =>
                {
                    t.Name = t.Name.SafeTrim();
                    t.Group = t.Group.SafeTrim();
                    t.Calendar = t.Calendar.SafeTrim();
                });
            metadata.CronTriggers?.ForEach(t =>
            {
                t.Name = t.Name.SafeTrim();
                t.Group = t.Group.SafeTrim();
                t.Calendar = t.Calendar.SafeTrim();
            });

            #endregion Trim

            #region Mandatory

            metadata.SimpleTriggers?.ForEach(t =>
            {
                if (string.IsNullOrEmpty(t.Name)) throw new PlannerValidationException("trigger name is mandatory");
            });
            metadata.CronTriggers?.ForEach(t =>
            {
                if (string.IsNullOrEmpty(t.Name)) throw new PlannerValidationException("trigger name is mandatory");
            });

            #endregion Mandatory

            #region Valid Name & Group

            var regex = new Regex(nameRegex);
            metadata.SimpleTriggers?.ForEach(t =>
            {
                if (IsRegexMatch(regex, t.Name) == false) throw new PlannerValidationException($"trigger name '{t.Name}' is invalid. use only alphanumeric, dashes & underscore");
                if (IsRegexMatch(regex, t.Group) == false) throw new PlannerValidationException($"trigger group '{t.Group}' is invalid. use only alphanumeric, dashes & underscore");
            });
            metadata.CronTriggers?.ForEach(t =>
            {
                if (IsRegexMatch(regex, t.Name) == false) throw new PlannerValidationException($"trigger name '{t.Name}' is invalid. use only alphanumeric, dashes & underscore");
                if (IsRegexMatch(regex, t.Group) == false) throw new PlannerValidationException($"trigger group '{t.Group}' is invalid. use only alphanumeric, dashes & underscore");
            });

            #endregion Valid Name & Group

            #region Max Chars

            metadata.SimpleTriggers?.ForEach(t =>
            {
                if (t.Name.Length > 50) throw new PlannerValidationException("trigger name length is invalid. max length is 50");
                if (t.Group?.Length > 50) throw new PlannerValidationException("trigger group length is invalid. max length is 50");
            });
            metadata.CronTriggers?.ForEach(t =>
            {
                if (t.Name.Length > 50) throw new PlannerValidationException("trigger name length is invalid. max length is 50");
                if (t.Group?.Length > 50) throw new PlannerValidationException("trigger group length is invalid. max length is 50");
            });

            #endregion Max Chars

            #region Preserve Words

            metadata.SimpleTriggers?.ForEach(t =>
            {
                if (t.Group == Consts.RetryTriggerGroup) { throw new PlannerValidationException($"simple trigger group '{Consts.RetryTriggerGroup}' is invalid (preserved value)"); }
            });
            metadata.CronTriggers?.ForEach(t =>
            {
                if (t.Group == Consts.RetryTriggerGroup) { throw new PlannerValidationException($"cron trigger group '{Consts.RetryTriggerGroup}' is invalid (preserved value)"); }
            });

            #endregion Preserve Words
        }

        private static JobMetadata GetJobMetadata(string yaml)
        {
            var deserializer = new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .Build();
            var metadata = deserializer.Deserialize<JobMetadata>(yaml);
            return metadata;
        }

        private static void AddPathRelativeFolder(JobMetadata metadata, string basePath)
        {
            const string pathField = "JobPath";
            if (metadata.Properties != null)
            {
                if (metadata.Properties.ContainsKey(pathField))
                {
                    var path = metadata.Properties[pathField];
                    if (string.IsNullOrEmpty(path) == false)
                    {
                        path = path.Trim();
                        if (path.Length > 0)
                        {
                            if (path == "." || path == @"\" || path == "/") { path = $"{basePath}"; }
                            else if (path[0] == '.')
                            {
                                path = $"{basePath}{path[1..]}";
                            }
                            else if (path[0] == '/' || path[0] == '\\')
                            {
                                path = @$"{basePath}\{path[1..]}";
                            }
                        }
                    }

                    metadata.Properties[pathField] = path;
                }
            }
        }

        private static Type GetJobType(JobMetadata job)
        {
            Assembly assembly;
            string typeName;

            switch (job.JobType)
            {
                case nameof(PlannerJob):
                case nameof(PlannerJobConcurrent):
                    try
                    {
                        assembly = Assembly.Load(nameof(RunPlannerJob));
                        typeName = $"{nameof(RunPlannerJob)}.{job.JobType}";
                    }
                    catch (Exception ex)
                    {
                        throw new PlannerValidationException($"Fail to load assemly {nameof(RunPlannerJob)}", ex);
                    }
                    break;

                default:
                    throw new PlannerValidationException($"Job type '{job.JobType}' is not supported");
            }

            try
            {
                var type = assembly.GetType(typeName);
                if (type == null) throw new PlannerValidationException($"Type {typeName} is not supported");
                return type;
            }
            catch (Exception ex)
            {
                throw new PlannerValidationException($"Fail to get type {job.JobType} from assemly {assembly.FullName}", ex);
            }
        }
    }
}
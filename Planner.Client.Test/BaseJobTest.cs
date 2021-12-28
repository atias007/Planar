using Planner.Common.Test;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Planner.Client.Test
{
    public abstract class BaseJobTest
    {
        protected static JobInstanceLog ExecuteJob<T>(Dictionary<string, object> dataMap = null, Dictionary<string, string> settings = null, DateTime? overrideNow = null)
        where T : BaseJob
        {
            var instance = Activator.CreateInstance<T>();
            if (overrideNow.HasValue)
            {
                dataMap.Add(Consts.NowOverrideValue, overrideNow);
            }

            var context = new MockJobExecutionContext(dataMap);
            instance.LoadJobSettings(settings);

            Exception jobException = null;
            var start = DateTime.Now;

            try
            {
                context.FireTimeUtc = new DateTimeOffset(overrideNow ?? DateTime.Now);
                context.FireInstanceId = $"JobTest_{Environment.MachineName}_{Environment.UserName}_{GenerateFireInstanceId()}";
                instance.Execute(context).Wait();
            }
            catch (Exception ex)
            {
                jobException = ex;
            }
            finally
            {
                context.JobRunTime = DateTime.Now.Subtract(start);
            }

            var data = context.MergedJobDataMap.Keys.Count == 0 ? null : JsonSerializer.Serialize(context.MergedJobDataMap);
            var duration = context.JobRunTime.TotalMilliseconds;
            var endDate = context.FireTimeUtc.DateTime.Add(context.JobRunTime);
            var status = jobException == null ? 0 : 1;

            //var value = context.Get(Consts.JobEffectedRows);
            //var effectedRows = value == null ? 0 : Convert.ToInt32(value);

            //value = context.Get(Consts.JobInformation);
            //var information = value == null ? null : Convert.ToString(value);
            // TODO: expose informaion to test

            var log = new JobInstanceLog
            {
                InstanceId = context.FireInstanceId,
                Data = data,
                StartDate = context.FireTimeUtc.DateTime,
                JobName = context.JobDetail.Key.Name,
                JobGroup = context.JobDetail.Key.Group,
                JobId = context.JobDetail.JobDataMap.GetString(Consts.JobId),
                TriggerName = context.Trigger.Key.Name,
                TriggerGroup = context.Trigger.Key.Group,
                TriggerId = context.Trigger.JobDataMap.GetString(Consts.TriggerId),
                Duration = Convert.ToInt32(duration),
                EndDate = endDate,
                Exception = jobException?.ToString(),
                // EffectedRows = effectedRows,
                // Information = information,
                // Id = 0,
                // IsStopped = false,
                // Retry = false,
                // StatusTitle = string.Empty
                Status = status
            };

            return log;
        }

        private static string GenerateFireInstanceId()
        {
            var result = new StringBuilder();
            var random = new Random();
            var offset = '0';
            for (var i = 0; i < 18; i++)
            {
                var @char = (char)random.Next(offset, offset + 10);
                result.Append(@char);
            }

            return result.ToString();
        }
    }
}
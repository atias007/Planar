using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Planar.Job.Test
{
    public abstract class BaseJobTest
    {
        protected abstract void Configure(IConfigurationBuilder configurationBuilder);

        protected abstract void RegisterServices(IServiceCollection services);

        protected static JobInstanceLog ExecuteJob<T>(Dictionary<string, string> dataMap = null, DateTime? overrideNow = null)
        where T : BaseJob
        {
            var instance = Activator.CreateInstance<T>();
            if (overrideNow.HasValue)
            {
                dataMap.Add(Consts.NowOverrideValue, Convert.ToString(overrideNow));
            }

            var dict = dataMap == null ? new SortedDictionary<string, string>() : new SortedDictionary<string, string>(dataMap);
            var context = new MockJobExecutionContext(new SortedDictionary<string, string>(dict));
            // TODO: instance.LoadJobSettings(settings);

            Exception jobException = null;
            var start = DateTime.Now;

            try
            {
                context.FireTime = new DateTimeOffset(overrideNow ?? DateTime.Now);
                context.FireInstanceId = $"JobTest_{Environment.MachineName}_{Environment.UserName}_{GenerateFireInstanceId()}";
                instance.ExecuteJob(context).Wait();
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
            var endDate = context.FireTime.DateTime.Add(context.JobRunTime);
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
                StartDate = context.FireTime.DateTime,
                JobName = context.JobDetail.Key.Name,
                JobGroup = context.JobDetail.Key.Group,
                JobId = context.JobDetail.JobDataMap[Consts.JobId],
                TriggerName = context.TriggerDetails.Key.Name,
                TriggerGroup = context.TriggerDetails.Key.Group,
                TriggerId = context.TriggerDetails.TriggerDataMap[Consts.TriggerId],
                Duration = Convert.ToInt32(duration),
                EndDate = endDate,
                Exception = jobException?.ToString(),
                // TODO: ---------------------------------
                // EffectedRows = effectedRows,
                // Information = information,
                // Id = 0,
                // IsStopped = false,
                // Retry = false,
                // StatusTitle = string.Empty
                Status = status,
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
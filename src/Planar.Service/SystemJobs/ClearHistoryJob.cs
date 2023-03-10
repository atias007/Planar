using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs
{
    public class ClearHistoryJob : SystemJob, IJob
    {
        private readonly ILogger<ClearHistoryJob> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ClearHistoryJob(IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<ClearHistoryJob>>();
            _serviceProvider = serviceProvider;
        }

        public Task Execute(IJobExecutionContext context)
        {
            return SafeDoWork();
        }

        public static async Task Schedule(IScheduler scheduler, CancellationToken stoppingToken = default)
        {
            const string description = "System job for clearing history records from database";
            var span = TimeSpan.FromHours(24);
            var start = DateTime.Now.Date.AddDays(1);
            await Schedule<ClearHistoryJob>(scheduler, description, span, start, stoppingToken);
        }

        private async Task SafeDoWork()
        {
            await Task.WhenAll(
                ClearTrace(),
                ClearJobLog(),
                ClearStatistics(),
                ClearProperties()
                );
        }

        private async Task ClearStatistics()
        {
            try
            {
                var data = _serviceProvider.GetRequiredService<StatisticsData>();
                await data.ClearStatisticsTables(AppSettings.ClearStatisticsTablesOverDays);
                _logger.LogInformation("Clear statistics tables rows (older then {Days} days)", AppSettings.ClearStatisticsTablesOverDays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to clear statistics tables rows (older then {Days} days)", AppSettings.ClearStatisticsTablesOverDays);
            }
        }

        private async Task ClearJobLog()
        {
            try
            {
                var data = _serviceProvider.GetRequiredService<TraceData>();
                await data.ClearJobLogTable(AppSettings.ClearJobLogTableOverDays);
                _logger.LogInformation("Clear job log table rows (older then {Days} days)", AppSettings.ClearJobLogTableOverDays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to clear job log table rows (older then {Days} days)", AppSettings.ClearJobLogTableOverDays);
            }
        }

        private async Task ClearTrace()
        {
            try
            {
                var data = _serviceProvider.GetRequiredService<TraceData>();
                await data.ClearTraceTable(AppSettings.ClearTraceTableOverDays);
                _logger.LogInformation("Clear trace table rows (older then {Days} days)", AppSettings.ClearTraceTableOverDays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to clear trace table rows (older then {Days} days)", AppSettings.ClearTraceTableOverDays);
            }
        }

        private async Task ClearProperties()
        {
            try
            {
                var data = _serviceProvider.GetRequiredService<JobData>();
                var ids = await data.GetJobPropertiesIds();
                var scheduler = _serviceProvider.GetRequiredService<IScheduler>();
                var existsKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
                var filterKeys = existsKeys.Where(x => x.Group != Consts.PlanarSystemGroup).ToList();
                var existsIds = filterKeys.Select(k => JobKeyHelper.GetJobId(scheduler.GetJobDetail(k).Result)).ToList();

                foreach (var id in ids)
                {
                    if (!existsIds.Contains(id))
                    {
                        await data.DeleteJobProperty(id);
                        _logger.LogDebug("delete job property for job id {JobId}", id);
                    }
                }
                _logger.LogInformation("Clear properties table rows");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to clear properties table rows");
            }
        }
    }
}
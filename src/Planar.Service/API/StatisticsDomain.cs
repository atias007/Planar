using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using Planar.Service.Model.DataObjects;
using Planar.Service.SystemJobs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public class StatisticsDomain : BaseJobBL<StatisticsDomain, StatisticsData>
    {
        public StatisticsDomain(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task RebuildJobStatistics()
        {
            var key = $"{Consts.PlanarSystemGroup}.{typeof(StatisticsJob).Name}";
            var jobKey = await JobKeyHelper.GetJobKey(key);
            await Scheduler.TriggerJob(jobKey);
        }

        public async Task<JobStatistic> GetJobStatistics(string jobId)
        {
            var key = await JobKeyHelper.GetJobKey(jobId);
            var id = await JobKeyHelper.GetJobId(key);

            using var scope1 = _serviceProvider.CreateScope();
            using var scope2 = _serviceProvider.CreateScope();
            using var scope3 = _serviceProvider.CreateScope();
            var query1 = scope1.ServiceProvider.GetRequiredService<StatisticsData>().GetJobDurationStatistics(id!);
            var query2 = scope2.ServiceProvider.GetRequiredService<StatisticsData>().GetJobEffectedRowsStatistics(id!);
            var s3 = scope2.ServiceProvider.GetRequiredService<StatisticsData>().GetJobCounters(id!);

            var s1 = Mapper.ProjectTo<JobDurationStatisticDto>(query1).FirstOrDefaultAsync();
            var s2 = Mapper.ProjectTo<JobEffectedRowsStatisticDto>(query2).FirstOrDefaultAsync();

            await Task.WhenAll(s1, s2, s3);

            var result = new JobStatistic();

            if (s1 != null) { Mapper.Map(s1.Result, result); }
            if (s2 != null) { Mapper.Map(s2.Result, result); }
            if (s3 != null) { Mapper.Map(s3.Result, result); }

            return result;
        }

        public async Task<JobCounters?> GetAllJobsCounters(AllJobsCountersRequest request)
        {
            var fromDate = request.FromDate ?? DateTime.Now.Date.AddDays(-1);
            return await DataLayer.GetAllJobsCounters(fromDate);
        }

        public async Task<IEnumerable<ConcurrentExecutionModel>> GetConcurrentExecution(ConcurrentExecutionRequest request)
        {
            ResetRequestHours(request);
            var query = DataLayer.GetConcurrentExecution(request);
            var result = await Mapper.ProjectTo<ConcurrentExecutionModel>(query).ToListAsync();
            return result;
        }

        public async Task<MaxConcurrentExecution> GetMaxConcurrentExecution(MaxConcurrentExecutionRequest request)
        {
            ResetRequestHours(request);
            var max = await DataLayer.GetMaxConcurrentExecution(request);
            var cluster = Resolve<ClusterDomain>();
            var total = await cluster.MaxConcurrency();
            var percentage = max * 1.0 / total;

            var result = new MaxConcurrentExecution
            {
                Value = max
            };

            if (percentage < 0.7) { result.Status = "Ok"; }
            if (percentage >= 0.7 && percentage < 0.9) { result.Status = "Warning"; }
            if (percentage >= 0.9) { result.Status = "Error"; }
            return result;
        }

        private static void ResetRequestHours(MaxConcurrentExecutionRequest request)
        {
            if (request.FromDate.HasValue)
            {
                var value = request.FromDate.Value;
                request.FromDate = value.Date.AddHours(value.Hour);
            }

            if (request.ToDate.HasValue)
            {
                var value = request.ToDate.Value;
                request.ToDate = value.Date.AddHours(value.Hour);
            }
        }
    }
}
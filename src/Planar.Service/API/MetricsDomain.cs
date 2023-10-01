using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using Planar.Service.Model;
using Planar.Service.Model.DataObjects;
using Planar.Service.SystemJobs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public class MetricsDomain : BaseJobBL<MetricsDomain, MetricsData>
    {
        public MetricsDomain(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task RebuildJobStatistics()
        {
            var key = $"{Consts.PlanarSystemGroup}.{typeof(StatisticsJob).Name}";
            var jobKey = await JobKeyHelper.GetJobKey(key);
            await Scheduler.TriggerJob(jobKey);
        }

        public async Task<JobMetrics> GetJobMetrics(string jobId)
        {
            var key = await JobKeyHelper.GetJobKey(jobId);
            var id = await JobKeyHelper.GetJobId(key);

            using var scope1 = _serviceProvider.CreateScope();
            using var scope2 = _serviceProvider.CreateScope();
            using var scope3 = _serviceProvider.CreateScope();
            var query1 = scope1.ServiceProvider.GetRequiredService<MetricsData>().GetJobDurationStatistics(id!);
            var query2 = scope2.ServiceProvider.GetRequiredService<MetricsData>().GetJobEffectedRowsStatistics(id!);
            var s3 = scope2.ServiceProvider.GetRequiredService<MetricsData>().GetJobCounters(id!);

            var s1 = Mapper.ProjectTo<JobDurationStatisticDto>(query1).FirstOrDefaultAsync();
            var s2 = Mapper.ProjectTo<JobEffectedRowsStatisticDto>(query2).FirstOrDefaultAsync();

            await Task.WhenAll(s1, s2, s3);

            var result = new JobMetrics();

            if (s1 != null) { Mapper.Map(s1.Result, result); }
            if (s2 != null) { Mapper.Map(s2.Result, result); }
            if (s3 != null) { Mapper.Map(s3.Result, result); }

            return result;
        }

        public async Task<JobCounters?> GetAllJobsCounters(AllJobsCountersRequest request)
        {
            return await DataLayer.GetAllJobsCounters(request.FromDate);
        }

        public async Task<PagingResponse<ConcurrentExecutionModel>> GetConcurrentExecution(ConcurrentExecutionRequest request)
        {
            ResetRequestHours(request);
            var query = DataLayer.GetConcurrentExecution(request);
            var result = await query.ProjectToWithPagingAsyc<ConcurrentExecution, ConcurrentExecutionModel>(Mapper, request);
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
                Value = max,
                Maximum = total
            };

            if (percentage < 0.7) { result.Status = "Ok"; }
            if (percentage >= 0.7 && percentage < 0.9) { result.Status = "Warning"; }
            if (percentage >= 0.9) { result.Status = "Error"; }
            return result;
        }

        private static void ResetRequestHours(IDateScope request)
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
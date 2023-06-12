using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using Planar.Service.Model.DataObjects;
using System;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public class StatisticsDomain : BaseJobBL<StatisticsDomain, StatisticsData>
    {
        public StatisticsDomain(IServiceProvider serviceProvider) : base(serviceProvider)
        {
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
    }
}
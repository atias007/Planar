using Microsoft.EntityFrameworkCore;
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

            var query1 = DataLayer.GetJobDurationStatistics(id!);
            var query2 = DataLayer.GetJobEffectedRowsStatistics(id!);

            var s1 = await Mapper.ProjectTo<JobDurationStatisticDto>(query1).FirstOrDefaultAsync();
            var s2 = await Mapper.ProjectTo<JobEffectedRowsStatisticDto>(query2).FirstOrDefaultAsync();

            var result = new JobStatistic();

            if (s1 != null) { Mapper.Map(s1, result); }
            if (s2 != null) { Mapper.Map(s2, result); }

            return result;
        }
    }
}
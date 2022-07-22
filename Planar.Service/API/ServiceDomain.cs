using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.General;
using Planar.Service.Model;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public class ServiceDomain : BaseBL<ServiceDomain>
    {
        public ServiceDomain(ILogger<ServiceDomain> logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        public async Task<GetServiceInfoResponse> GetServiceInfo()
        {
            var totalJobs = Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            var totalGroups = Scheduler.GetJobGroupNames();
            var metadata = Scheduler.GetMetaData();

            var response = new GetServiceInfoResponse
            {
                InStandbyMode = Scheduler.InStandbyMode,
                IsShutdown = Scheduler.IsShutdown,
                IsStarted = Scheduler.IsStarted,
                Environment = Global.Environment,
                TotalJobs = (await totalJobs).Count,
                TotalGroups = (await totalGroups).Count,
                Clustered = (await metadata).JobStoreClustered,
                JobStoreType = (await metadata).JobStoreType.FullName,
                RunningSince = (await metadata).RunningSince.GetValueOrDefault().DateTime,
                QuartzVersion = (await metadata).Version
            };

            return response;
        }

        public async Task<List<string>> GetCalendars()
        {
            var list = (await Scheduler.GetCalendarNames()).ToList();
            return list;
        }

        public async Task StopScheduler()
        {
            await SchedulerUtil.Stop();
            await new ClusterUtil(DataLayer, Logger).StopScheduler();
        }

        public async Task StartScheduler()
        {
            await SchedulerUtil.Start();
            await new ClusterUtil(DataLayer, Logger).StartScheduler();
        }

        public async Task<List<ClusterNode>> GetNodes()
        {
            return await DataLayer.GetClusterNodes();
        }
    }
}
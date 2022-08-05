using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.General;
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
                Clustering = (await metadata).JobStoreClustered,
                DatabaseProvider = (await metadata).JobStoreType.FullName,
                RunningSince = (await metadata).RunningSince.GetValueOrDefault().DateTime,
                QuartzVersion = (await metadata).Version,
                ClusteringCheckinInterval = AppSettings.ClusteringCheckinInterval,
                ClusteringCheckinMisfireThreshold = AppSettings.ClusteringCheckinMisfireThreshold,
                ClusterPort = AppSettings.ClusterPort,
                ClearTraceTableOverDays = AppSettings.ClearTraceTableOverDays,
                HttpPort = AppSettings.HttpPort,
                HttpsPort = AppSettings.HttpsPort,
                MaxConcurrency = AppSettings.MaxConcurrency,
                PersistRunningJobsSpan = AppSettings.PersistRunningJobsSpan,
                UseHttps = AppSettings.UseHttps,
                UseHttpsRedirect = AppSettings.UseHttpsRedirect,
                ServiceVersion = ServiceVersion
            };

            return response;
        }

        public async Task<bool> HealthCheck()
        {
            var hc = SchedulerUtil.IsSchedulerRunning;
            if (!hc) { return false; }
            if (AppSettings.Clustering)
            {
                hc = await new ClusterUtil(DataLayer, Logger).HealthCheck();
            }

            return hc;
        }

        public async Task<List<string>> GetCalendars()
        {
            var list = (await Scheduler.GetCalendarNames()).ToList();
            return list;
        }

        public async Task StopScheduler()
        {
            await SchedulerUtil.Stop();
            if (AppSettings.Clustering)
            {
                await new ClusterUtil(DataLayer, Logger).StopScheduler();
            }
        }

        public async Task StartScheduler()
        {
            await SchedulerUtil.Start();
            if (AppSettings.Clustering)
            {
                await new ClusterUtil(DataLayer, Logger).StartScheduler();
            }
        }
    }
}
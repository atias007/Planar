using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Planar.Service.General.Hash;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
                RunningSince = (await metadata).RunningSince?.DateTime,
                QuartzVersion = (await metadata).Version,
                ClusteringCheckinInterval = AppSettings.ClusteringCheckinInterval,
                ClusteringCheckinMisfireThreshold = AppSettings.ClusteringCheckinMisfireThreshold,
                ClusterPort = AppSettings.ClusterPort,
                ClearTraceTableOverDays = AppSettings.ClearTraceTableOverDays,
                HttpPort = AppSettings.HttpPort,
                HttpsPort = AppSettings.HttpsPort,
                MaxConcurrency = AppSettings.MaxConcurrency,
                UseHttps = AppSettings.UseHttps,
                UseHttpsRedirect = AppSettings.UseHttpsRedirect,
                ServiceVersion = ServiceVersion,
                LogLevel = AppSettings.LogLevel.ToString(),
                SwaggerUI = AppSettings.SwaggerUI,
                DeveloperExceptionPage = AppSettings.DeveloperExceptionPage,
                AuthenticationMode = AppSettings.AuthenticationMode.ToString()
            };

            return response;
        }

        public async Task<string> GetServiceInfo(string key)
        {
            var lowerKey = key.Replace(" ", string.Empty).ToLower();
            var info = await GetServiceInfo();
            var props = info.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var prop = props.FirstOrDefault(p => p.Name.ToLower() == lowerKey);

            if (prop == null)
            {
                throw new RestNotFoundException($"Key '{key}' was not found in service information");
            }

            var value = prop.GetValue(info);
            return Convert.ToString(value);
        }

        public async Task<bool> HealthCheck()
        {
            var hc = SchedulerUtil.IsSchedulerRunning;
            if (!hc) { return false; }

            if (AppSettings.Clustering)
            {
                var util = _serviceProvider.GetRequiredService<ClusterUtil>();
                hc = await util.HealthCheck();
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
                var util = _serviceProvider.GetRequiredService<ClusterUtil>();
                await util.StopScheduler();
            }
        }

        public async Task StartScheduler()
        {
            await SchedulerUtil.Start();
            if (AppSettings.Clustering)
            {
                var util = _serviceProvider.GetRequiredService<ClusterUtil>();
                await util.StartScheduler();
            }
        }

        public async Task<string> Login(LoginRequest request)
        {
            var user = await DataLayer.GetUserByUsername(request.Username);
            ValidateExistingEntity(user);

            var verify = HashUtil.VerifyHash(request.Password, user.Password, user.Salt);
            if (!verify)
            {
                throw new RestValidationException("password", "Wrong password");
            }

            return user.Id.ToString();
        }
    }
}
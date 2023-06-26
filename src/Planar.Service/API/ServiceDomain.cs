using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Planar.Service.General.Hash;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public class ServiceDomain : BaseBL<ServiceDomain, ServiceData>
    {
        public ServiceDomain(IServiceProvider serviceProvider) : base(serviceProvider)
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
                Environment = AppSettings.Environment,
                TotalJobs = (await totalJobs).Count,
                TotalGroups = (await totalGroups).Count,
                Clustering = (await metadata).JobStoreClustered,
                DatabaseProvider = (await metadata).JobStoreType.FullName ?? Consts.Undefined,
                RunningSince = (await metadata).RunningSince?.DateTime,
                QuartzVersion = (await metadata).Version,
                JobAutoStopSpan = AppSettings.JobAutoStopSpan,
                ClusteringCheckinInterval = AppSettings.ClusteringCheckinInterval,
                ClusteringCheckinMisfireThreshold = AppSettings.ClusteringCheckinMisfireThreshold,
                ClusterPort = AppSettings.ClusterPort,
                ClearTraceTableOverDays = AppSettings.ClearTraceTableOverDays,
                ClearJobLogTableOverDays = AppSettings.ClearJobLogTableOverDays,
                ClearStatisticsTablesOverDays = AppSettings.ClearStatisticsTablesOverDays,
                HttpPort = AppSettings.HttpPort,
                HttpsPort = AppSettings.HttpsPort,
                MaxConcurrency = AppSettings.MaxConcurrency,
                UseHttps = AppSettings.UseHttps,
                UseHttpsRedirect = AppSettings.UseHttpsRedirect,
                ServiceVersion = ServiceVersion ?? Consts.Undefined,
                LogLevel = AppSettings.LogLevel.ToString(),
                SwaggerUI = AppSettings.SwaggerUI,
                OpenApiUI = AppSettings.OpenApiUI,
                DeveloperExceptionPage = AppSettings.DeveloperExceptionPage,
                AuthenticationMode = AppSettings.AuthenticationMode.ToString(),
                AuthenticationTokenExpire = AppSettings.AuthenticationTokenExpire,
            };

            return response;
        }

        public async Task<string?> GetServiceInfo(string key)
        {
            var lowerKey = key.Replace(" ", string.Empty).ToLower();
            var info = await GetServiceInfo();
            var props = info.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var prop =
                Array.Find(props, p => p.Name.ToLower() == lowerKey) ??
                throw new RestNotFoundException($"key '{key}' was not found in service information");

            var value = prop.GetValue(info);
            return PlanarConvert.ToString(value);
        }

        public async Task<string> HealthCheck()
        {
            var serviceUnavaliable = false;
            var result = new StringBuilder();

            var hc = SchedulerUtil.IsSchedulerRunning;
            if (hc)
            {
                result.AppendLine("Scheduler healthy");
            }
            else
            {
                serviceUnavaliable = true;
                result.AppendLine("Scheduler unhealthy");
            }

            try
            {
                await DataLayer.HealthCheck();
                result.AppendLine("Database healthy");
            }
            catch (Exception ex)
            {
                serviceUnavaliable = false;
                result.AppendLine($"Database unhealthy: {ex.Message}");
            }

            if (AppSettings.Clustering)
            {
                var util = _serviceProvider.GetRequiredService<ClusterUtil>();
                hc = await util.HealthCheck();

                if (hc)
                {
                    result.AppendLine("Cluster healthy");
                }
                else
                {
                    serviceUnavaliable = true;
                    result.AppendLine("Cluster unhealthy");
                }
            }
            else
            {
                result.AppendLine("Cluster: [Clustering not enabled, skip health check]");
            }

            var message = result.ToString().Trim();
            if (serviceUnavaliable)
            {
                throw new RestServiceUnavailableException(message);
            }

            return message;
        }

        public async Task<List<string>> GetCalendars()
        {
            var list = (await Scheduler.GetCalendarNames()).ToList();
            return list;
        }

        public async Task HaltScheduler()
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

        public async Task<LoginResponse> Login(LoginRequest request)
        {
            if (AppSettings.AuthenticationMode == AuthMode.AllAnonymous)
            {
                throw new RestConflictException("login service is not avaliable when authentication mode is disabled (AllAnonymous)");
            }

            if (string.IsNullOrWhiteSpace(request.Username))
            {
                throw new RestValidationException("username", "username is required");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new RestValidationException("password", "password is required");
            }

            var userData = Resolve<UserData>();
            var user =
                await userData.GetUserIdentity(request.Username) ??
                throw new RestValidationException("username", $"user with username '{request.Username}' not exists", 100);

            var role = await userData.GetUserRole(user.Id);
            user.RoleId = role;

            var verify = HashUtil.VerifyHash(request.Password!, user.Password, user.Salt);
            if (!verify)
            {
                throw new RestValidationException("password", "wrong password", 101);
            }

            var token = HashUtil.CreateToken(user);
            var result = new LoginResponse
            {
                Role = RoleHelper.GetTitle(role),
                Token = token,
                FirstName = user.Surename, 
                LastName = user.GivenName,
            };

            return result;
        }
    }
}
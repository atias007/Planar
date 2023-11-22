using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Calendars;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Planar.Service.General.Hash;
using Planar.Service.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public class ServiceDomain : BaseBL<ServiceDomain, ServiceData>
    {
        public ServiceDomain(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public string GetServiceVersion()
        {
            return ServiceVersion ?? Consts.Undefined;
        }

        public async Task<AppSettingsInfo> GetServiceInfo()
        {
            const string scrt = "********";
            var result = new AppSettingsInfo
            {
                General = Mapper.Map<GeneralSettingsInfo>(AppSettings.General),
                Database = Mapper.Map<DatabaseSettingsInfo>(AppSettings.Database),
                Authentication = Mapper.Map<AuthenticationSettingsInfo>(AppSettings.Authentication),
                Cluster = Mapper.Map<ClusterSettingsInfo>(AppSettings.Cluster),
                Retention = Mapper.Map<RetentionSettingsInfo>(AppSettings.Retention),
                Smtp = Mapper.Map<SmtpSettingsInfo>(AppSettings.Smtp),
                Monitor = Mapper.Map<MonitorSettingsInfo>(AppSettings.Monitor),
            };

            if (UserRole != Roles.Administrator)
            {
                result.Database.ConnectionString = scrt;
                result.Smtp.Username = scrt;
                result.Smtp.Password = scrt;
            }

            return await Task.FromResult(result);
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

            if (AppSettings.Cluster.Clustering)
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
            if (AppSettings.Cluster.Clustering)
            {
                var util = _serviceProvider.GetRequiredService<ClusterUtil>();
                await util.StopScheduler();
            }
        }

        public async Task StartScheduler()
        {
            await SchedulerUtil.Start();
            if (AppSettings.Cluster.Clustering)
            {
                var util = _serviceProvider.GetRequiredService<ClusterUtil>();
                await util.StartScheduler();
            }
        }

        public async Task<LoginResponse> Login(LoginRequest request)
        {
            if (AppSettings.Authentication.NoAuthontication)
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
            var user = await userData.GetUserIdentity(request.Username);
            if (user == null)
            {
                AuditSecuritySafe($"login fail with not exists username '{request.Username}'", isWarning: true);
                throw new RestValidationException("username", $"user with username '{request.Username}' not exists", 100);
            }

            var role = await userData.GetUserRole(user.Id);
            user.RoleId = role;

            var verify = HashUtil.VerifyHash(request.Password!, user.Password, user.Salt);
            if (!verify)
            {
                AuditSecuritySafe($"user '{user.Fullname}' try to login with username '{request.Username}' and with wrong password", isWarning: true);
                throw new RestValidationException("password", "wrong password", 101);
            }

            var roleTitle = RoleHelper.GetTitle(role);
            AuditSecuritySafe($"user '{user.Fullname}' with username '{request.Username}' and role '{roleTitle}' successfully login");

            var token = HashUtil.CreateToken(user);
            var result = new LoginResponse
            {
                Role = roleTitle,
                Token = token,
                FirstName = user.Surename,
                LastName = user.GivenName,
            };

            return result;
        }

        public async Task<PagingResponse<SecurityAuditModel>> GetSecurityAudits([FromQuery] SecurityAuditsFilter request)
        {
            var query = DataLayer.GetSecurityAudits(request);
            var data = await query.ProjectToWithPagingAsyc<SecurityAudit, SecurityAuditModel>(Mapper, request);
            var result = new PagingResponse<SecurityAuditModel>(data);
            return result;
        }

        public IEnumerable<WorkingHoursModel> GetDefaultWorkingHours()
        {
            if (!WorkingHours.Calendars.Any()) { throw new RestNotFoundException("no working hours found in settings file"); }

            var result = new List<WorkingHoursModel>();
            foreach (var item in WorkingHours.Calendars)
            {
                result.Add(MapWorkingHours(item));
            }

            return result;
        }

        public WorkingHoursModel GetWorkingHours(string calendar)
        {
            if (!CalendarInfo.Contains(calendar))
            {
                throw new RestNotFoundException($"calendar '{calendar}' not found");
            }

            var cal =
                WorkingHours.GetCalendar(calendar) ??
                throw new RestNotFoundException($"working hours for calendar '{calendar}' not defined. planar will use default working hours");

            var result = MapWorkingHours(cal!);
            return result;
        }

        private WorkingHoursModel MapWorkingHours(WorkingHoursCalendar cal)
        {
            var result = new WorkingHoursModel { CalendarName = cal.CalendarName };
            foreach (var d in cal.Days)
            {
                var dayModel = new WorkingHoursDayModel
                {
                    DayOfWeek = d.DayOfWeek ?? string.Empty,
                    Scopes = Mapper.Map<List<WorkingHourScopeModel>>(d.DefaultScopes ? cal.DefaultScopes : d.Scopes)
                };

                result.Days.Add(dayModel);
            }

            return result;
        }
    }
}
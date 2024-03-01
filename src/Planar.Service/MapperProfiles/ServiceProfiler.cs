using AutoMapper;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Calendars;
using Planar.Service.Model;

namespace Planar.Service.MapperProfiles;

internal class ServiceProfiler : Profile
{
    public ServiceProfiler()
    {
        CreateMap<SecurityAudit, SecurityAuditModel>();
        CreateMap<GeneralSettings, GeneralSettingsInfo>();
        CreateMap<DatabaseSettings, DatabaseSettingsInfo>();
        CreateMap<AuthenticationSettings, AuthenticationSettingsInfo>();
        CreateMap<ClusterSettings, ClusterSettingsInfo>();
        CreateMap<SmtpSettings, SmtpSettingsInfo>();
        CreateMap<MonitorSettings, MonitorSettingsInfo>();
        CreateMap<RetentionSettings, RetentionSettingsInfo>();
        CreateMap<WorkingHourScope, WorkingHourScopeModel>();
    }
}
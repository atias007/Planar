using AutoMapper;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Model;

namespace Planar.Service.MapperProfiles
{
    internal class ServiceProfiler : Profile
    {
        public ServiceProfiler()
        {
            CreateMap<SecurityAudit, SecurityAuditModel>();

            CreateMap<GeneralSettings, GeneralSettingsInfo>();
        }
    }
}
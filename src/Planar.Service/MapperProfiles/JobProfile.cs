using AutoMapper;
using Planar.API.Common.Entities;
using Planar.Service.Model;

namespace Planar.Service.MapperProfiles
{
    internal class JobProfile : Profile
    {
        public JobProfile()
        {
            CreateMap<SetJobRequest<PlanarJobProperties>, SetJobDynamicRequest>();
            CreateMap<SetJobRequest<ProcessJobProperties>, SetJobDynamicRequest>();
            CreateMap<JobAudit, JobAuditWithInfoDto>();
            CreateMap<JobAudit, JobAuditDto>();
        }
    }
}
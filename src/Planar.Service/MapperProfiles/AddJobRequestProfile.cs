using AutoMapper;
using Planar.API.Common.Entities;
using Planar.Service.Model;

namespace Planar.Service.MapperProfiles
{
    internal class AddJobRequestProfile : Profile
    {
        public AddJobRequestProfile()
        {
            CreateMap<SetJobRequest<PlanarJobProperties>, SetJobDynamicRequest>();
        }
    }
}
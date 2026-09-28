using AutoMapper;
using Planar.Service.Model;

namespace Planar.Service.MapperProfiles
{
    internal class ResourceProfile : Profile
    {
        public ResourceProfile()
        {
            CreateMap<Resource, ResourceModel>().ReverseMap();
            CreateMap<Resource, ApplyResourceRequest>().ReverseMap();
        }
    }
}
using AutoMapper;
using Planar.API.Common.Entities;
using Planar.Service.Model;

namespace Planar.Service.MapperProfiles
{
    internal class GroupProfile : Profile
    {
        public GroupProfile()
        {
            CreateMap<Group, GroupDetails>()
                .ForMember(t => t.Role, map => map.MapFrom(s => s.Role.Name));

            CreateMap<AddGroupRequest, Group>();

            CreateMap<UpdateGroupRequest, Group>().ReverseMap();
        }
    }
}
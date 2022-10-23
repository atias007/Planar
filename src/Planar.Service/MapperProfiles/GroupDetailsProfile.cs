using AutoMapper;
using Planar.API.Common.Entities;
using Planar.Service.Model;

namespace Planar.Service.MapperProfiles
{
    internal class GroupDetailsProfile : Profile
    {
        public GroupDetailsProfile()
        {
            CreateMap<Group, GroupDetails>()
                .ForMember(t => t.Role, map => map.MapFrom(s => s.Role.Name));
        }
    }
}
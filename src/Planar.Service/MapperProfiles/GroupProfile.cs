using AutoMapper;
using Planar.API.Common.Entities;
using Planar.Service.Model;
using System;

namespace Planar.Service.MapperProfiles;

internal class GroupProfile : Profile
{
    public GroupProfile()
    {
        CreateMap<Group, GroupDetails>()
            .ForMember(t => t.Role, map => map.MapFrom(s => s.Role.Name));

        CreateMap<AddGroupRequest, Group>()
            .ForMember(r => r.RoleId, map => map.MapFrom(s => (int)Enum.Parse<Roles>(s.Role ?? string.Empty)))
            .ForMember(r => r.Role, map => map.Ignore());

        CreateMap<UpdateGroupRequest, Group>()
            .ForMember(r => r.Role, map => map.Ignore())
            .ReverseMap();
    }
}
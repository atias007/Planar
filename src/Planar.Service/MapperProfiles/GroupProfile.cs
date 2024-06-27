using AutoMapper;
using Planar.API.Common.Entities;
using Planar.Service.Model;
using System;

namespace Planar.Service.MapperProfiles;

internal class GroupProfile : Profile
{
    public GroupProfile()
    {
        CreateMap<Group, GroupDetails>();

        CreateMap<AddGroupRequest, Group>()
            .ForMember(r => r.RoleId, map => map.MapFrom(s => (int)Enum.Parse<Roles>(s.Role ?? string.Empty, true)));

        CreateMap<UpdateGroupRequest, Group>()
            .ReverseMap();
    }
}
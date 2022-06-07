using AutoMapper;
using Planar.API.Common.Entities;
using Planar.Service.Model;

namespace Planar.Service.MapperProfiles
{
    internal class GroupDetailsProfile : Profile
    {
        public GroupDetailsProfile()
        {
            CreateMap<Group, GroupDetails>();
        }
    }
}
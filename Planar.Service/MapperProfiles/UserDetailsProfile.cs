using AutoMapper;
using Planar.API.Common.Entities;
using Planar.Service.Model;

namespace Planar.Service.MapperProfiles
{
    internal class UserDetailsProfile : Profile
    {
        public UserDetailsProfile()
        {
            CreateMap<User, UserDetails>()
                .ForMember(t => t.Password, map => map.MapFrom(s => string.Empty.PadLeft(15, '*')));
        }
    }
}
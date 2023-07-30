using AutoMapper;
using Planar.API.Common.Entities;
using Planar.Service.Model;

namespace Planar.Service.MapperProfiles
{
    internal class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<User, UserDetails>();

            CreateMap<User, UserRowModel>();

            CreateMap<AddUserRequest, User>();

            CreateMap<UpdateUserRequest, User>().ReverseMap();
        }
    }
}
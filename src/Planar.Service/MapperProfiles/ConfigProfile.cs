using AutoMapper;
using Planar.API.Common.Entities;
using Planar.Service.Model;

namespace Planar.Service.MapperProfiles;

internal class ConfigProfile : Profile
{
    public ConfigProfile()
    {
        CreateMap<GlobalConfig, GlobalConfigModel>().ReverseMap();
    }
}
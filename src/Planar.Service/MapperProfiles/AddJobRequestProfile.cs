using AutoMapper;
using Planar.API.Common.Entities;
using Planar.Service.Model;

namespace Planar.Service.MapperProfiles
{
    internal class AddJobRequestProfile<TProperties> : Profile
        where TProperties : class, new()
    {
        public AddJobRequestProfile()
        {
            CreateMap<AddJobRequest<TProperties>, AddJobDynamicRequest>();
        }
    }
}
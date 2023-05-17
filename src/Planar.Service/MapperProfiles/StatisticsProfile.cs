using AutoMapper;
using Planar.Service.Model;
using Planar.Service.Model.DataObjects;

namespace Planar.Service.MapperProfiles
{
    internal class StatisticsProfile : Profile
    {
        public StatisticsProfile()
        {
            CreateMap<JobInstanceLog, JobInstanceLogForStatistics>().ReverseMap();
        }
    }
}
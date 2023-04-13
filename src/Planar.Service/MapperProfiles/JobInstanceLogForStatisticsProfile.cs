using AutoMapper;
using Planar.Service.Model;
using Planar.Service.Model.DataObjects;

namespace Planar.Service.MapperProfiles
{
    internal class JobInstanceLogForStatisticsProfile : Profile
    {
        public JobInstanceLogForStatisticsProfile()
        {
            CreateMap<JobInstanceLog, JobInstanceLogForStatistics>().ReverseMap();
        }
    }
}
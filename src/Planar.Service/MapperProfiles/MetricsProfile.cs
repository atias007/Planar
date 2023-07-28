using AutoMapper;
using Planar.API.Common.Entities;
using Planar.Service.Model;
using Planar.Service.Model.DataObjects;
using System;

namespace Planar.Service.MapperProfiles
{
    internal class MetricsProfile : Profile
    {
        public MetricsProfile()
        {
            CreateMap<JobInstanceLog, JobInstanceLogForStatistics>().ReverseMap();
            CreateMap<JobDurationStatistic, JobDurationStatisticDto>().ReverseMap();
            CreateMap<JobEffectedRowsStatisticDto, JobEffectedRowsStatistic>().ReverseMap();
            CreateMap<JobDurationStatisticDto, JobMetrics>()
                .ForMember(t => t.StdevDuration, map => map.MapFrom(s => TimeSpan.FromMilliseconds(Convert.ToDouble(s.StdevDuration))))
                .ForMember(t => t.AvgDuration, map => map.MapFrom(s => TimeSpan.FromMilliseconds(Convert.ToDouble(s.AvgDuration))));
            CreateMap<JobEffectedRowsStatisticDto, JobMetrics>();
            CreateMap<JobCounters, JobMetrics>();
            CreateMap<ConcurrentExecution, ConcurrentExecutionModel>();
        }
    }
}
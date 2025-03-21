using AutoMapper;
using Planar.API.Common.Entities;
using Planar.Service.Model;

namespace Planar.Service.MapperProfiles;

internal class HistoryProfile : Profile
{
    public HistoryProfile()
    {
        CreateMap<JobInstanceLog, JobInstanceLogRow>();
        CreateMap<JobInstanceLog, JobHistory>();
        CreateMap<HistoryLastLog, JobLastRun>();
    }
}
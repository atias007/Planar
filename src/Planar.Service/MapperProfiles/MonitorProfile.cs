using AutoMapper;
using Planar.API.Common.Entities;
using Planar.Service.Model;

namespace Planar.Service.MapperProfiles
{
    internal class MonitorProfile : Profile
    {
        public MonitorProfile()
        {
            CreateMap<MonitorAction, MonitorItem>()
             .ForMember(t => t.Active, map => map.MapFrom(s => s.Active.GetValueOrDefault()))
             .ForMember(t => t.EventTitle, map => map.MapFrom(s => ((MonitorEvents)s.EventId).ToString()));

            CreateMap<AddMonitorRequest, MonitorAction>()
                .Include<UpdateMonitorRequest, MonitorAction>()
                .ForMember(t => t.Active, map => map.MapFrom(s => true))
                .ForMember(t => t.JobGroup, map => map.MapFrom(s => string.IsNullOrEmpty(s.JobGroup) ? null : s.JobGroup))
                .ForMember(t => t.EventArgument, map => map.MapFrom(s => string.IsNullOrEmpty(s.EventArgument) ? null : s.EventArgument));

            CreateMap<UpdateMonitorRequest, MonitorAction>().ReverseMap();
        }
    }
}
using AutoMapper;
using Planar.API.Common.Entities;
using Planar.Service.General;
using Planar.Service.Model;

namespace Planar.Service.MapperProfiles
{
    internal class MonitorProfile : Profile
    {
        public MonitorProfile()
        {
            CreateMap<MonitorAction, MonitorItem>()
             .ForMember(t => t.Active, map => map.MapFrom(s => s.Active.GetValueOrDefault()))
             .ForMember(t => t.EventTitle, map => map.MapFrom(s => ((MonitorEvents)s.EventId).ToString().SplitWords()))
             .ForMember(t => t.DistributionGroupName, map => map.MapFrom(s => s.Group.Name));

            CreateMap<AddMonitorRequest, MonitorAction>()
                .Include<UpdateMonitorRequest, MonitorAction>()
                .ForMember(t => t.EventId, map => map.MapFrom(s => s.EventId.GetValueOrDefault()))
                .ForMember(t => t.JobGroup, map => map.MapFrom(s => string.IsNullOrEmpty(s.JobGroup) ? null : s.JobGroup))
                .ForMember(t => t.EventArgument, map => map.MapFrom(s => string.IsNullOrEmpty(s.EventArgument) ? null : s.EventArgument));

            CreateMap<UpdateMonitorRequest, MonitorAction>().ReverseMap();
        }
    }
}
using AutoMapper;
using Planar.API.Common.Entities;
using Planar.Service.Model;
using System.Collections.Generic;

namespace Planar.Service.MapperProfiles
{
    internal class MonitorProfile : Profile
    {
        public MonitorProfile()
        {
            CreateMap<MonitorAction, MonitorItem>()
             .ForMember(t => t.Active, map => map.MapFrom(s => s.Active.GetValueOrDefault()))
             .ForMember(t => t.EventTitle, map => map.MapFrom(s => ((MonitorEvents)s.EventId).ToString()))
             .ForMember(t => t.GroupName, map => map.MapFrom(s => s.Group.Name))
             .ForMember(t => t.Job, map => map.MapFrom(s => string.IsNullOrEmpty(s.JobGroup) ? $"Id: {s.JobId}" : $"Group: {s.JobGroup}"));

            //CreateMap<List<MonitorAction>, List<MonitorItem>>();

            CreateMap<AddMonitorRequest, MonitorAction>()
                //.Include<UpdateMonitorRequest, MonitorAction>()
                .ForMember(t => t.Active, map => map.MapFrom(s => true))
                .ForMember(t => t.JobGroup, map => map.MapFrom(s => string.IsNullOrEmpty(s.JobGroup) ? null : s.JobGroup))
                .ForMember(t => t.EventArgument, map => map.MapFrom(s => string.IsNullOrEmpty(s.EventArgument) ? null : s.EventArgument));

            CreateMap<UpdateMonitorRequest, MonitorAction>();
        }
    }
}
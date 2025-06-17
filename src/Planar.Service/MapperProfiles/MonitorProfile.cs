using AutoMapper;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Model;
using Planar.Service.Monitor;
using System;
using System.Linq;

namespace Planar.Service.MapperProfiles;

public class MonitorProfile : Profile
{
    public MonitorProfile()
    {
        CreateMap<AddMonitorRequest, MonitorAction>()
            .ForMember(t => t.EventId, map => map.MapFrom(s => (int)Enum.Parse<MonitorEvents>(s.Event ?? string.Empty)))
            .ForMember(t => t.JobGroup, map => map.MapFrom(s => string.IsNullOrEmpty(s.JobGroup) ? null : s.JobGroup))
            .ForMember(t => t.EventArgument, map => map.MapFrom(s => string.IsNullOrEmpty(s.EventArgument) ? null : s.EventArgument));

        CreateMap<MonitorAction, MonitorItem>()
         .ForMember(t => t.Event, map => map.MapFrom(s => MonitorUtil.GetMonitorEventTitle(s.EventId, s.EventArgument)))
         .ForMember(t => t.DistributionGroups, map => map.MapFrom(s => s.Groups.Select(s => s.Name)));

        CreateMap<UpdateMonitorRequest, MonitorAction>()
            .ForMember(t => t.EventId, map => map.MapFrom(s => (int)Enum.Parse<MonitorEvents>(s.Event ?? string.Empty)))
            .ForMember(t => t.JobGroup, map => map.MapFrom(s => string.IsNullOrEmpty(s.JobGroup) ? null : s.JobGroup))
            .ForMember(t => t.EventArgument, map => map.MapFrom(s => string.IsNullOrEmpty(s.EventArgument) ? null : s.EventArgument));

        CreateMap<MonitorAction, UpdateMonitorRequest>()
            .ForMember(t => t.Event, map => map.MapFrom(s => ((MonitorEvents)s.EventId).ToString()))
            .ForMember(t => t.JobGroup, map => map.MapFrom(s => string.IsNullOrEmpty(s.JobGroup) ? null : s.JobGroup))
            .ForMember(t => t.EventArgument, map => map.MapFrom(s => string.IsNullOrEmpty(s.EventArgument) ? null : s.EventArgument));

        CreateMap<MonitorAlert, MonitorAlertRowModel>();
        CreateMap<MonitorAlert, MonitorAlertModel>();
        CreateMap<MonitorHook, MonitorHookDetails>().ReverseMap();

        CreateMap<MonitorMute, MuteItem>();
        CreateMap<MonitorCounter, MuteItem>()
            .ForMember(d => d.DueDate, map => map.MapFrom(s => (s.LastUpdate ?? DateTime.Now).Add(AppSettings.Monitor.MaxAlertsPeriod)));

        CreateMap<HookWrapper, HookInfo>()
            .ForMember(d => d.HookType, map => map.MapFrom(s => s.HookType.ToString()));
    }
}
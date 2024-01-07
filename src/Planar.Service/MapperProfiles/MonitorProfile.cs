using AutoMapper;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Model;
using Planar.Service.Monitor;
using System;

namespace Planar.Service.MapperProfiles;

public class MonitorProfile : Profile
{
    public MonitorProfile()
    {
        CreateMap<AddMonitorRequest, MonitorAction>()
            .ForMember(t => t.EventId, map => map.MapFrom(s => (int)Enum.Parse<MonitorEvents>(s.EventName ?? string.Empty)))
            .ForMember(t => t.JobGroup, map => map.MapFrom(s => string.IsNullOrEmpty(s.JobGroup) ? null : s.JobGroup))
            .ForMember(t => t.EventArgument, map => map.MapFrom(s => string.IsNullOrEmpty(s.EventArgument) ? null : s.EventArgument));

        CreateMap<MonitorAction, MonitorItem>()
         .ForMember(t => t.Active, map => map.MapFrom(s => s.Active.GetValueOrDefault()))
         .ForMember(t => t.EventTitle, map => map.MapFrom(s => ((MonitorEvents)s.EventId).GetEnumDescription()));

        CreateMap<UpdateMonitorRequest, MonitorAction>()
            .ForMember(t => t.EventId, map => map.MapFrom(s => (int)Enum.Parse<MonitorEvents>(s.EventName ?? string.Empty)))
            .ForMember(t => t.JobGroup, map => map.MapFrom(s => string.IsNullOrEmpty(s.JobGroup) ? null : s.JobGroup))
            .ForMember(t => t.EventArgument, map => map.MapFrom(s => string.IsNullOrEmpty(s.EventArgument) ? null : s.EventArgument));

        CreateMap<MonitorAction, UpdateMonitorRequest>()
            .ForMember(t => t.EventName, map => map.MapFrom(s => ((MonitorEvents)s.EventId).ToString()))
            .ForMember(t => t.JobGroup, map => map.MapFrom(s => string.IsNullOrEmpty(s.JobGroup) ? null : s.JobGroup))
            .ForMember(t => t.EventArgument, map => map.MapFrom(s => string.IsNullOrEmpty(s.EventArgument) ? null : s.EventArgument));

        CreateMap<MonitorAlert, MonitorAlertRowModel>();
        CreateMap<MonitorAlert, MonitorAlertModel>();

        CreateMap<MonitorDetails, MonitorAlert>()
            .ForMember(d => d.GroupId, map => map.MapFrom(s => s.Group == null ? 0 : s.Group.Id))
            .ForMember(d => d.GroupName, map => map.MapFrom(s => s.Group == null ? string.Empty : s.Group.Name))
            .ForMember(d => d.UsersCount, map => map.MapFrom(s => s.Users == null ? 0 : s.Users.Count))
            .ForMember(d => d.AlertDate, map => map.MapFrom(s => DateTime.Now));

        CreateMap<MonitorSystemDetails, MonitorAlert>()
            .ForMember(d => d.GroupId, map => map.MapFrom(s => s.Group == null ? 0 : s.Group.Id))
            .ForMember(d => d.GroupName, map => map.MapFrom(s => s.Group == null ? string.Empty : s.Group.Name))
            .ForMember(d => d.UsersCount, map => map.MapFrom(s => s.Users == null ? 0 : s.Users.Count))
            .ForMember(d => d.AlertDate, map => map.MapFrom(s => DateTime.Now));

        CreateMap<MonitorMute, MuteItem>();
        CreateMap<MonitorCounter, MuteItem>()
            .ForMember(d => d.DueDate, map => map.MapFrom(s => (s.LastUpdate ?? DateTime.Now).Add(AppSettings.Monitor.MaxAlertsPeriod)));

        CreateMap<HookWrapper, HookInfo>()
            .ForMember(d => d.HookType, map => map.MapFrom(s => s.HookType.ToString()));
    }
}
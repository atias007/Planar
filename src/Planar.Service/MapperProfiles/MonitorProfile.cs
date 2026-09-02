using AutoMapper;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Model;
using Planar.Service.Monitor;
using System;
using System.Linq;
using System.Text.Json;

namespace Planar.Service.MapperProfiles;

public class MonitorProfile : Profile
{
#pragma warning disable S3776 // Cognitive Complexity of methods should not be too high

    public MonitorProfile()
#pragma warning restore S3776 // Cognitive Complexity of methods should not be too high
    {
        CreateMap<MonitorAction, UpdateMonitorRequest>()
            .ForMember(t => t.Event, map => map.MapFrom(s => ((MonitorEvents)s.EventId).ToString()))
            .ForMember(t => t.JobGroup, map => map.MapFrom(s => string.IsNullOrEmpty(s.JobGroup) ? null : s.JobGroup))
            .ForMember(t => t.EventArguments, map => map.MapFrom(s => string.IsNullOrEmpty(s.EventArgument) ? null : s.EventArgument));

        CreateMap<MonitorAlert, MonitorAlertRowModel>();
        CreateMap<MonitorAlert, MonitorAlertModel>();
        CreateMap<MonitorHook, MonitorHookDetails>().ReverseMap();

        CreateMap<MonitorMute, MuteItem>();
        CreateMap<MonitorCounter, MuteItem>()
            .ForMember(d => d.DueDate, map => map.MapFrom(s => (s.LastUpdate ?? DateTime.Now).Add(AppSettings.Monitor.MaxAlertsPeriod)));

        CreateMap<HookWrapper, HookInfo>()
            .ForMember(d => d.HookType, map => map.MapFrom(s => s.HookType.ToString()));
    }

    public static MonitorItem ToMonitorItem(MonitorAction action)
    {
        var item = new MonitorItem
        {
            Id = action.Id,
            JobGroup = string.IsNullOrEmpty(action.JobGroup) ? null : action.JobGroup,
            JobName = string.IsNullOrEmpty(action.JobName) ? null : action.JobName,
            Title = action.Title,
            Active = action.Active,
            EventArguments = action.EventArgument,
            Event = MonitorUtil.GetMonitorEventTitle(action.EventId),
            DistributionGroups = action.Groups.Select(g => g.Name),
            Hooks = action.MonitorActionsHooks.Select(h => h.Hook),
            EventId = action.EventId
        };

        return item;
    }

    public static void SetMonitorAction(MonitorAction action, ApplyMonitorRequest request)
    {
        _ = MonitorEventsParser.TryParse(request.Event, out var eventId);

        action.JobGroup = string.IsNullOrEmpty(request.JobGroup) ? null : request.JobGroup;
        action.JobName = string.IsNullOrEmpty(request.JobName) ? null : request.JobName;
        action.Title = request.Title;
        action.EventArgument = request.ToEventArgumentString();
        action.EventId = (int)eventId;
        action.Active = request.Active;
    }

    public static MonitorAction ToMonitorAction(AddMonitorRequest request)
    {
        var action = new MonitorAction
        {
            JobGroup = string.IsNullOrEmpty(request.JobGroup) ? null : request.JobGroup,
            JobName = string.IsNullOrEmpty(request.JobName) ? null : request.JobName,
            Title = request.Title,
            EventArgument = request.ToEventArgumentString()
        };

        _ = MonitorEventsParser.TryParse(request.Event, out var eventId);
        action.EventId = (int)eventId;

        return action;
    }

    public static MonitorAction ToMonitorAction(UpdateMonitorRequest request)
    {
        var action = new MonitorAction
        {
            JobGroup = string.IsNullOrEmpty(request.JobGroup) ? null : request.JobGroup,
            JobName = string.IsNullOrEmpty(request.JobName) ? null : request.JobName,
            Title = request.Title,
            EventArgument = request.ToEventArgumentString(),
            Id = request.Id
        };

        _ = MonitorEventsParser.TryParse(request.Event, out var eventId);
        action.EventId = (int)eventId;

        return action;
    }

    public static MonitorAction ToMonitorAction(ApplyMonitorRequest request)
    {
        var action = new MonitorAction
        {
            JobGroup = string.IsNullOrEmpty(request.JobGroup) ? null : request.JobGroup,
            JobName = string.IsNullOrEmpty(request.JobName) ? null : request.JobName,
            Title = request.Title,
            Active = request.Active,
            EventArgument = request.ToEventArgumentString()
        };

        _ = MonitorEventsParser.TryParse(request.Event, out var eventId);
        action.EventId = (int)eventId;

        return action;
    }

    public static UpdateMonitorRequest ToUpdateMonitorRequest(MonitorAction action)
    {
        var request = new UpdateMonitorRequest
        {
            JobGroup = string.IsNullOrEmpty(action.JobGroup) ? null : action.JobGroup,
            JobName = string.IsNullOrEmpty(action.JobName) ? null : action.JobName,
            Title = action.Title,
            EventArguments = action.GetEventArguments(),
            Id = action.Id
        };

        var eventId = (MonitorEvents)action.EventId;
        request.Event = eventId.ToString();

        return request;
    }
}
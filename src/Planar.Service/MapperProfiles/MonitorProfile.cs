using AutoMapper;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Data;
using Planar.Service.Model;
using Planar.Service.Monitor;
using System;

namespace Planar.Service.MapperProfiles
{
    public class MonitorProfile : Profile
    {
        public MonitorProfile(GroupData groupDal)
        {
            CreateMap<AddMonitorRequest, MonitorAction>()
                .Include<UpdateMonitorRequest, MonitorAction>()
                .ForMember(t => t.EventId, map => map.MapFrom(s => (int)Enum.Parse<MonitorEvents>(s.EventName ?? string.Empty)))
                .ForMember(t => t.JobGroup, map => map.MapFrom(s => string.IsNullOrEmpty(s.JobGroup) ? null : s.JobGroup))
                .ForMember(t => t.EventArgument, map => map.MapFrom(s => string.IsNullOrEmpty(s.EventArgument) ? null : s.EventArgument))
                .ForMember(t => t.GroupId, map => map.MapFrom(s => groupDal.GetGroupId(s.GroupName).Result));

            CreateMap<MonitorAction, MonitorItem>()
             .ForMember(t => t.Active, map => map.MapFrom(s => s.Active.GetValueOrDefault()))
             .ForMember(t => t.EventTitle, map => map.MapFrom(s => ((MonitorEvents)s.EventId).GetEnumDescription()))
             .ForMember(t => t.DistributionGroupName, map => map.MapFrom(s => s.Group.Name));

            CreateMap<UpdateMonitorRequest, MonitorAction>().ReverseMap();

            CreateMap<MonitorAlert, MonitorAlertRowModel>();

            CreateMap<MonitorDetails, MonitorAlert>()
                .ForMember(d => d.GroupId, map => map.MapFrom(s => s.Group == null ? 0 : s.Group.Id))
                .ForMember(d => d.GroupName, map => map.MapFrom(s => s.Group == null ? string.Empty : s.Group.Name))
                .ForMember(d => d.UsersCount, map => map.MapFrom(s => s.Users == null ? 0 : s.Users.Count))
                .ForMember(d => d.AlertDate, map => map.MapFrom(s => DateTime.Now))
                ;

            CreateMap<MonitorSystemDetails, MonitorAlert>()
                .ForMember(d => d.GroupId, map => map.MapFrom(s => s.Group == null ? 0 : s.Group.Id))
                .ForMember(d => d.GroupName, map => map.MapFrom(s => s.Group == null ? string.Empty : s.Group.Name))
                .ForMember(d => d.UsersCount, map => map.MapFrom(s => s.Users == null ? 0 : s.Users.Count))
                .ForMember(d => d.AlertDate, map => map.MapFrom(s => DateTime.Now))
                ;
        }
    }
}
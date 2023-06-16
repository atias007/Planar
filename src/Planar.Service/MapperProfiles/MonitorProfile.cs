using AutoMapper;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Data;
using Planar.Service.Model;
using System;

namespace Planar.Service.MapperProfiles
{
    internal class MonitorProfile : Profile
    {
        public MonitorProfile(GroupData groupDal)
        {
            CreateMap<MonitorAction, MonitorItem>()
             .ForMember(t => t.Active, map => map.MapFrom(s => s.Active.GetValueOrDefault()))
             .ForMember(t => t.EventTitle, map => map.MapFrom(s => ((MonitorEvents)s.EventId).GetEnumDescription()))
             .ForMember(t => t.DistributionGroupName, map => map.MapFrom(s => s.Group.Name));

            CreateMap<AddMonitorRequest, MonitorAction>()
                .Include<UpdateMonitorRequest, MonitorAction>()
                .ForMember(t => t.EventId, map => map.MapFrom(s => (int)Enum.Parse<MonitorEvents>(s.EventName ?? string.Empty)))
                .ForMember(t => t.JobGroup, map => map.MapFrom(s => string.IsNullOrEmpty(s.JobGroup) ? null : s.JobGroup))
                .ForMember(t => t.EventArgument, map => map.MapFrom(s => string.IsNullOrEmpty(s.EventArgument) ? null : s.EventArgument))
                .ForMember(t => t.GroupId, map => map.MapFrom(s => groupDal.GetGroupId(s.GroupName).Result));

            CreateMap<UpdateMonitorRequest, MonitorAction>().ReverseMap();
        }
    }
}
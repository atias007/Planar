using AutoMapper;
using CronExpressionDescriptor;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Common.Helpers;
using Planar.Service.API.Helpers;
using Quartz;
using System;

namespace Planar.Service.MapperProfiles;

internal class TriggerDetailsProfile : Profile
{
    public TriggerDetailsProfile()
    {
    }

    public TriggerDetailsProfile(IScheduler scheduler, JobKeyHelper jobKeyHelper)
    {
        CreateMap<ITrigger, PausedTriggerDetails>()
            .Include<ITrigger, TriggerDetails>()
            .ForMember(t => t.Id, map => map.MapFrom(s => GetTriggerId(s)))
            .ForMember(t => t.TriggerName, map => map.MapFrom(s => s.Key.Name))
            .ForMember(t => t.JobName, map => map.MapFrom(s => s.JobKey.Name))
            .ForMember(t => t.JobGroup, map => map.MapFrom(s => s.JobKey.Group))
            .ForMember(t => t.JobId, map => map.MapFrom(s => jobKeyHelper.GetJobId(s.JobKey).Result));

        CreateMap<ITrigger, TriggerDetails>()
            .Include<ISimpleTrigger, SimpleTriggerDetails>()
            .Include<ICronTrigger, CronTriggerDetails>()
            .ForMember(t => t.Id, map => map.MapFrom(s => GetTriggerId(s)))
            .ForMember(t => t.TriggerName, map => map.MapFrom(s => s.Key.Name))
            .ForMember(t => t.RetrySpan, map => map.MapFrom(s => TriggerHelper.GetRetrySpan(s)))
            .ForMember(t => t.MaxRetries, map => map.MapFrom(s => TriggerHelper.GetMaxRetries(s)))
            .ForMember(t => t.Timeout, map => map.MapFrom(s => TriggerHelper.GetTimeout(s)))
            .ForMember(t => t.End, map => map.MapFrom(s => GetDateTime(s.EndTimeUtc)))
            .ForMember(t => t.Start, map => map.MapFrom(s => GetDateTime(s.StartTimeUtc)))
            .ForMember(t => t.FinalFire, map => map.MapFrom(s => GetDateTime(s.FinalFireTimeUtc)))
            .ForMember(t => t.MayFireAgain, map => map.MapFrom(s => s.GetMayFireAgain()))
            .ForMember(t => t.NextFireTime, map => map.MapFrom(s => GetDateTime(s.GetNextFireTimeUtc())))
            .ForMember(t => t.PreviousFireTime, map => map.MapFrom(s => GetDateTime(s.GetPreviousFireTimeUtc())))
            .ForMember(t => t.DataMap, map => map.MapFrom(s => Global.ConvertDataMapToDictionary(s.JobDataMap)))
            .ForMember(t => t.State, map => map.MapFrom(s => GetTriggerState(s.Key, scheduler)));

        CreateMap<ISimpleTrigger, SimpleTriggerDetails>()
            .ForMember(t => t.MisfireBehaviour, map => map.MapFrom(s => GetMisfireInstructionNameForSimpleTrigger(s.MisfireInstruction)));

        CreateMap<ICronTrigger, CronTriggerDetails>()
            .ForMember(t => t.CronExpression, map => map.MapFrom(s => s.CronExpressionString))
            .ForMember(t => t.CronDescription, map => map.MapFrom(s => GetCronDescription(s.CronExpressionString)))
            .ForMember(t => t.MisfireBehaviour, map => map.MapFrom(s => GetMisfireInstructionNameForCronTrigger(s.MisfireInstruction)));
    }

    internal static string GetCronDescription(string? expression)
    {
        if (expression == null) { return string.Empty; }
        return ExpressionDescriptor.GetDescription(expression, new Options { Use24HourTimeFormat = true });
    }

    private static DateTime? GetDateTime(DateTimeOffset? dateTimeOffset)
    {
        return dateTimeOffset?.LocalDateTime;
    }

    private static string? GetTriggerState(TriggerKey key, IScheduler scheduler)
    {
        var result = scheduler.GetTriggerState(key).Result;
        return Convert.ToString(result);
    }

    private static string GetTriggerId(ITrigger trigger)
    {
        if (trigger.Key.Group == Consts.RecoveringJobsGroup)
        {
            return Consts.RecoveringJobsGroup;
        }

        var id = TriggerHelper.GetTriggerId(trigger);
        return id ?? Consts.Undefined;
    }

    private static string GetMisfireInstructionNameForSimpleTrigger(int value)
    {
        return value switch
        {
            -1 => "Ignore Misfire Policy",
            0 => "Instruction Not Set",
            1 => "Fire Now",
            2 => "Now With Existing Repeat Count",
            3 => "Now With Remaining Repeat Count",
            4 => "Next With Remaining Count",
            5 => "Next With Existing Count",
            _ => "Unknown",
        };
    }

    private static string GetMisfireInstructionNameForCronTrigger(int value)
    {
        return value switch
        {
            -1 => "Ignore Misfire Policy",
            0 => "Instruction Not Set",
            1 => "Fire Once Now",
            2 => "Do Nothing",
            _ => "Unknown",
        };
    }
}
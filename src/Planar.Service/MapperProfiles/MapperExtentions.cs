using AutoMapper;
using Planar.API.Common.Entities;
using Planar.Common.Helpers;
using Quartz;
using System;
using System.Threading.Tasks;
using static Quartz.MisfireInstruction;

namespace Planar.Service.MapperProfiles;

internal static class MapperExtentions
{
    public static async Task<SimpleTriggerDetails> MapSimpleTriggerDetails(this IMapper mapper, ISimpleTrigger simpleTrigger, IScheduler scheduler)
    {
        var result = mapper.Map<SimpleTriggerDetails>(simpleTrigger);

        var abs = ((Quartz.Impl.Triggers.AbstractTrigger)simpleTrigger);
        result.PreferedNode = abs.IsPreferredNodeAuto ? "[Auto]" : abs.PreferredNode;

        result.State = await GetTriggerState(simpleTrigger.Key, scheduler);
        result.Active = await GetTriggerActive(simpleTrigger.Key, scheduler);
        return result;
    }

    public static async Task<CronTriggerDetails> MapCronTriggerDetails(this IMapper mapper, ICronTrigger cronTrigger, IScheduler scheduler)
    {
        var result = mapper.Map<CronTriggerDetails>(cronTrigger);

        var abs = ((Quartz.Impl.Triggers.AbstractTrigger)cronTrigger);
        result.PreferedNode = abs.IsPreferredNodeAuto ? "[Auto]" : abs.PreferredNode;

        result.State = await GetTriggerState(cronTrigger.Key, scheduler);
        result.Active = await GetTriggerActive(cronTrigger.Key, scheduler);
        return result;
    }

    private static async Task<string?> GetTriggerState(TriggerKey key, IScheduler scheduler)
    {
        var result = await scheduler.GetTriggerState(key);
        return Convert.ToString(result);
    }

    private static async Task<bool> GetTriggerActive(TriggerKey key, IScheduler scheduler)
    {
        var state = await scheduler.GetTriggerState(key);
        var result = TriggerHelper.IsActiveState(state);
        return result;
    }
}
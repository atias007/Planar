using Quartz;

namespace Planar.Service.API.Helpers;

internal static class TriggerKeyHelper
{
    public static bool IsSystemTriggerKey(TriggerKey triggerKey)
    {
        return triggerKey.Group == Consts.PlanarSystemGroup;
    }
}
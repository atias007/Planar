using Planner.CLI.Attributes;
using System.Threading.Tasks;

namespace Planner.CLI.Actions
{
    [Module("monitor")]
    public class MonitorCliActions : BaseCliAction<MonitorCliActions>
    {
        [Action("reload")]
        public static async Task<ActionResponse> ReloadMonitor()
        {
            var result = await Proxy.InvokeAsync(x => x.ReloadMonitor());
            return new ActionResponse(result, mesage: result.Result);
        }

        [Action("hooks")]
        public static async Task<ActionResponse> GetMonitorHooks()
        {
            var result = await Proxy.InvokeAsync(x => x.GetMonitorHooks());
            return new ActionResponse(result, serializeObj: result.Result);
        }
    }
}
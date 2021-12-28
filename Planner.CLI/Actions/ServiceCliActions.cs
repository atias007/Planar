using Planner.CLI.Attributes;
using Planner.CLI.Entities;
using System;

using System.Threading.Tasks;

namespace Planner.CLI.Actions
{
    [Module("service")]
    public class ServiceCliActions : BaseCliAction<ServiceCliActions>
    {
        [Action("stop")]
        public static async Task<ActionResponse> StopScheduler(CliStopScheduler request)
        {
            var prm = new API.Common.Entities.StopSchedulerRequest
            {
                WaitJobsToComplete = !request.Force
            };

            var result = await Proxy.InvokeAsync(x => x.StopScheduler(prm));
            return new ActionResponse(result);
        }

        [Action("isalive")]
        public static async Task<ActionResponse> IsAlive()
        {
            var result = await Proxy.InvokeAsync(x => x.GetServiceInfo());
            var message = (result.IsStarted && result.IsShutdown == false && result.InStandbyMode == false).ToString().ToLower();
            return new ActionResponse(result, message);
        }

        [Action("env")]
        public static async Task<ActionResponse> GetEnvironment()
        {
            var result = await Proxy.InvokeAsync(x => x.GetServiceInfo());
            return new ActionResponse(result, result.Environment);
        }

        [Action("calendars")]
        public static async Task<ActionResponse> GetAllCalendars()
        {
            var result = await Proxy.InvokeAsync(x => x.GetAllCalendars());
            return new ActionResponse(result, serializeObj: result.Result);
        }
    }
}
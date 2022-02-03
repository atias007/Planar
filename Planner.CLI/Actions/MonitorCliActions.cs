using Planner.API.Common.Entities;
using Planner.CLI.Attributes;
using Planner.CLI.Entities;
using Spectre.Console;
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

        [Action("ls")]
        public static async Task<ActionResponse> GetMonitorItems(CliGetMonitorItemsRequest request)
        {
            var prm = JsonMapper.Map<GetMonitorItemsRequest, CliGetMonitorItemsRequest>(request);
            var result = await Proxy.InvokeAsync(x => x.GetMonitorItems(prm));
            var table = CliTableExtensions.GetTable(result);
            return new ActionResponse(result, table);
        }

        [Action("add")]
        public static async Task<ActionResponse> AddMonitorHooks()
        {
            var title = AnsiConsole.Ask<string>($"[turquoise2]  > Title: [/]");
            var jobId = AnsiConsole.Ask<string>($"[turquoise2]  > Job id: [/]");
            var jobGroup = AnsiConsole.Ask<string>($"[turquoise2]  > Job group: [/]");
            var monitorEvent = AnsiConsole.Ask<int>($"[turquoise2]  > Monitor event: [/]");
            var monitorEventArgs = AnsiConsole.Ask<int>($"[turquoise2]  > Monitor event argument: [/]");
            var groupId = AnsiConsole.Ask<int>($"[turquoise2]  > Distribution group: [/]");
            var hook = AnsiConsole.Ask<int>($"[turquoise2]  > Hook: [/]");

            //var result = await Proxy.InvokeAsync(x => x.GetMonitorHooks());
            return await Task.FromResult(new ActionResponse(null));
        }
    }
}
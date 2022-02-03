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
            var data = await Proxy.InvokeAsync(x => x.GetMonitorActionMedatada());

            var title = AnsiConsole.Prompt(new TextPrompt<string>("[turquoise2]  > Title: [/]")
                .Validate(title =>
                {
                    if (string.IsNullOrWhiteSpace(title)) { return ValidationResult.Error("[red]Title is required field[/]"); }
                    if (title.Length > 50) { return ValidationResult.Error("[red]Title limited to 50 chars maximum[/]"); }
                    if (title.Length < 5) { return ValidationResult.Error("[red]Title must have at least 5 chars[/]"); }
                    return ValidationResult.Success();
                }));

            var jobId = AnsiConsole.Prompt(new TextPrompt<string>("[turquoise2]  > Job id: [/]").AllowEmpty());
            var jobGroup = AnsiConsole.Prompt(new TextPrompt<string>("[turquoise2]  > Job group: [/]").AllowEmpty());
            var monitorEvent = AnsiConsole.Ask<int>("[turquoise2]  > Monitor event: [/]");
            var monitorEventArgs = AnsiConsole.Prompt(new TextPrompt<string>("[turquoise2]  > Monitor event argument: [/]").AllowEmpty());
            var groupId = AnsiConsole.Ask<int>("[turquoise2]  > Distribution group: [/]");

            var hookPrompt = new TextPrompt<string>("[turquoise2]  > Hook: [/]")
                .InvalidChoiceMessage("[red]That's not a valid hook[/]");

            data.Result.Hooks.ForEach(h => hookPrompt.AddChoice(h));
            var hook = AnsiConsole.Prompt(hookPrompt);

            //var result = await Proxy.InvokeAsync(x => x.GetMonitorHooks());
            return await Task.FromResult(new ActionResponse(null));
        }
    }
}
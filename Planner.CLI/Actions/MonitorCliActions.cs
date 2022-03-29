using Planner.API.Common.Entities;
using Planner.CLI.Attributes;
using Planner.CLI.Entities;
using Spectre.Console;
using System;
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
        public static async Task<ActionResponse> GetMonitorActions(CliGetMonitorActionsRequest request)
        {
            var prm = JsonMapper.Map<GetMonitorActionsRequest, CliGetMonitorActionsRequest>(request);
            var result = await Proxy.InvokeAsync(x => x.GetMonitorActions(prm));
            var table = CliTableExtensions.GetTable(result);
            return new ActionResponse(result, table);
        }

        [Action("add")]
        public static async Task<ActionResponse> AddMonitorHooks()
        {
            var data = await Proxy.InvokeAsync(x => x.GetMonitorActionMedatada());
            if (data.Success == false)
            {
                throw new ApplicationException(data.ErrorDescription);
            }

            // === Title ===
            var title = AnsiConsole.Prompt(new TextPrompt<string>("[turquoise2]  > Title: [/]")
                .Validate(title =>
                {
                    if (string.IsNullOrWhiteSpace(title)) { return ValidationResult.Error("[red]Title is required field[/]"); }
                    if (title.Length > 50) { return ValidationResult.Error("[red]Title limited to 50 chars maximum[/]"); }
                    if (title.Length < 5) { return ValidationResult.Error("[red]Title must have at least 5 chars[/]"); }
                    return ValidationResult.Success();
                }));

            // === JobId ===
            if (data.Result.Jobs != null && data.Result.Jobs.Count > 0)
            {
                var table = CliTableExtensions.GetTable(data.Result.Jobs, "Description");
                AnsiConsole.Write(table);
            }
            var jobId = AnsiConsole.Prompt(new TextPrompt<string>("[turquoise2]  > Job id: [/]").AllowEmpty());

            int jobGroup;
            // === JobGroup ===
            if (string.IsNullOrEmpty(jobId))
            {
                if (data.Result.JobGroups != null && data.Result.JobGroups.Count > 0)
                {
                    var table = CliTableExtensions.GetTable(data.Result.JobGroups, "Job Group");
                    AnsiConsole.Write(table);
                }
                jobGroup = AnsiConsole.Prompt(new TextPrompt<int>("[turquoise2]  > Job group id: [/]").AllowEmpty());
            }

            // === Event ===
            var evenTtable = CliTableExtensions.GetTable(data.Result.Events, "Event Name");
            AnsiConsole.Write(evenTtable);
            var monitorEvent = AnsiConsole.Ask<int>("[turquoise2]  > Monitor event id: [/]");

            // === EventArguments ===
            var monitorEventArgs =
                monitorEvent >= 10 ?
                AnsiConsole.Prompt(new TextPrompt<string>("[turquoise2]  > Monitor event argument: [/]").AllowEmpty()) :
                null;

            // === Distribution Group ===
            var groupsTable = CliTableExtensions.GetTable(data.Result.Groups, "Group Name");
            AnsiConsole.Write(groupsTable);
            var groupId = AnsiConsole.Ask<int>("[turquoise2]  > Distribution group id: [/]");

            // === Hook ===
            var hooksTable = CliTableExtensions.GetTable(data.Result.Hooks, "Name");
            AnsiConsole.Write(hooksTable);
            var hookPrompt = new TextPrompt<int>("[turquoise2]  > Hook id: [/]");
            var hookId = AnsiConsole.Prompt(hookPrompt);
            var hookName = data.Result.Hooks[hookId];

            var monitor = new AddMonitorRequest
            {
                EventArguments = monitorEventArgs,
                GroupId = groupId,
                Hook = hookName,
                JobId = jobId,
                MonitorEvent = monitorEvent,
                Title = title
            };

            var result = await Proxy.InvokeAsync(x => x.AddMonitor(monitor));
            return new ActionResponse(result);
        }

        [Action("events")]
        public static async Task<ActionResponse> GetMonitorEvents()
        {
            var result = await Proxy.InvokeAsync(x => x.GetMonitorEvents());
            return new ActionResponse(result, serializeObj: result.Result);
        }

        [Action("remove")]
        [Action("delete")]
        public static async Task<ActionResponse> DeleteMonitor(CliGetByIdRequest request)
        {
            var prm = JsonMapper.Map<GetByIdRequest, CliGetByIdRequest>(request);
            var result = await Proxy.InvokeAsync(x => x.DeleteMonitor(prm));
            return new ActionResponse(result);
        }
    }
}
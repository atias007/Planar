using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("monitor")]
    public class MonitorCliActions : BaseCliAction<MonitorCliActions>
    {
        [Action("reload")]
        public static async Task<CliActionResponse> ReloadMonitor()
        {
            var restRequest = new RestRequest("monitor/reload", Method.Post);
            var result = await RestProxy.Invoke<string>(restRequest);
            return new CliActionResponse(result, message: result.Data);
        }

        [Action("hooks")]
        public static async Task<CliActionResponse> GetMonitorHooks()
        {
            var restRequest = new RestRequest("monitor/hooks", Method.Get);
            var result = await RestProxy.Invoke<List<string>>(restRequest);
            return new CliActionResponse(result, serializeObj: result.Data);
        }

        [Action("ls")]
        public static async Task<CliActionResponse> GetMonitorActions(CliGetMonitorActionsRequest request)
        {
            RestRequest restRequest;

            if (string.IsNullOrEmpty(request.JobIdOrJobGroup))
            {
                restRequest = new RestRequest("monitor", Method.Get);
            }
            else
            {
                restRequest = new RestRequest("monitor/{jobOrGroupId}", Method.Get)
                    .AddParameter("jobOrGroupId", request.JobIdOrJobGroup, ParameterType.UrlSegment);
            }

            var result = await RestProxy.Invoke<List<MonitorItem>>(restRequest);
            var table = CliTableExtensions.GetTable(result.Data);
            return new CliActionResponse(result, table);
        }

        [Action("add")]
        public static async Task<CliActionResponse> AddMonitorHooks()
        {
            var restRequest = new RestRequest("monitor/metadata", Method.Get);
            var result = await RestProxy.Invoke<MonitorActionMedatada>(restRequest);
            if (result.IsSuccessful == false)
            {
                // TODO: show error like cli show error (central function to pull error from RestRequest
                throw new ApplicationException($"Error: {result.ErrorMessage}");
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
            if (result.Data.Jobs != null && result.Data.Jobs.Count > 0)
            {
                var table = CliTableExtensions.GetTable(result.Data.Jobs, "Description");
                AnsiConsole.Write(table);
            }
            var jobId = AnsiConsole.Prompt(new TextPrompt<string>("[turquoise2]  > Job id: [/]").AllowEmpty());

            int jobGroup;
            // === JobGroup ===
            if (string.IsNullOrEmpty(jobId))
            {
                if (result.Data.JobGroups != null && result.Data.JobGroups.Count > 0)
                {
                    var table = CliTableExtensions.GetTable(result.Data.JobGroups, "Job Group");
                    AnsiConsole.Write(table);
                }
                jobGroup = AnsiConsole.Prompt(new TextPrompt<int>("[turquoise2]  > Job group id: [/]").AllowEmpty());
            }

            // === Event ===
            var evenTtable = CliTableExtensions.GetTable(result.Data.Events, "Event Name");
            AnsiConsole.Write(evenTtable);
            var monitorEvent = AnsiConsole.Ask<int>("[turquoise2]  > Monitor event id: [/]");

            // === EventArguments ===
            var monitorEventArgs =
                monitorEvent >= 10 ?
                AnsiConsole.Prompt(new TextPrompt<string>("[turquoise2]  > Monitor event argument: [/]").AllowEmpty()) :
                null;

            // === Distribution Group ===
            var groupsTable = CliTableExtensions.GetTable(result.Data.Groups, "Group Name");
            AnsiConsole.Write(groupsTable);
            var groupId = AnsiConsole.Ask<int>("[turquoise2]  > Distribution group id: [/]");

            // === Hook ===
            var hooksTable = CliTableExtensions.GetTable(result.Data.Hooks, "Name");
            AnsiConsole.Write(hooksTable);
            var hookPrompt = new TextPrompt<int>("[turquoise2]  > Hook id: [/]");
            var hookId = AnsiConsole.Prompt(hookPrompt);
            var hookName = result.Data.Hooks[hookId];

            var monitor = new AddMonitorRequest
            {
                EventArguments = monitorEventArgs,
                GroupId = groupId,
                Hook = hookName,
                JobId = jobId,
                MonitorEvent = monitorEvent,
                Title = title
            };

            var restRequestAdd = new RestRequest("monitor", Method.Post)
                .AddBody(monitor);
            var resultAdd = await RestProxy.Invoke<int>(restRequestAdd);

            return new CliActionResponse(resultAdd, message: Convert.ToString(resultAdd.Data));
        }

        [Action("events")]
        public static async Task<CliActionResponse> GetMonitorEvents()
        {
            var restRequest = new RestRequest("monitor/events", Method.Get);
            var result = await RestProxy.Invoke<List<string>>(restRequest);
            return new CliActionResponse(result, serializeObj: result.Data);
        }

        [Action("remove")]
        [Action("delete")]
        public static async Task<CliActionResponse> DeleteMonitor(CliGetByIdRequest request)
        {
            var restRequest = new RestRequest("monitor/{id}", Method.Delete)
                .AddParameter("id", request.Id, ParameterType.UrlSegment);
            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }
    }
}
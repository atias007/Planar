using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using Planar.CLI.Exceptions;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    internal struct AddMonitorJobData
    {
        public string JobId { get; set; }
        public string JobGroupId { get; set; }

        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrEmpty(JobId) && string.IsNullOrEmpty(JobGroupId);
            }
        }
    }

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

        [Action("update")]
        public static async Task<CliActionResponse> UpdateMonitor(CliUpdateEntityRequest request)
        {
            var restRequest = new RestRequest("monitor/{id}", Method.Patch)
                .AddParameter("id", request.Id, ParameterType.UrlSegment)
                .AddBody(request);

            return await Execute(restRequest);
        }

        [Action("add")]
        public static async Task<CliActionResponse> AddMonitorAction()
        {
            var metadata = await GetMetadata();
            if (metadata.IsSuccessful == false)
            {
                return new CliActionResponse(metadata);
            }

            if (!metadata.Data.Hooks.Any())
            {
                throw new CliValidationException("there are no monitor hooks define in service");
            }

            if (!metadata.Data.Groups.Any())
            {
                throw new CliValidationException("there are no distribution groups define in service");
            }

            if (!metadata.Data.Jobs.Any())
            {
                throw new CliValidationException("there are no jobs define in service");
            }

            var title = GetTitle();
            var job = GetJob(metadata);
            var monitorEvent = GetEvent(metadata);
            var monitorEventArgs = GetEventArguments(monitorEvent);
            var groupId = GetDistributionGroup(metadata);
            var hookName = GetHook(metadata);

            AnsiConsole.Write(new Rule());

            var monitor = new AddMonitorRequest
            {
                EventArgument = monitorEventArgs,
                JobGroup = job.JobGroupId,
                GroupId = groupId,
                Hook = hookName,
                JobId = job.JobId,
                EventId = monitorEvent,
                Title = title
            };

            var restRequestAdd = new RestRequest("monitor", Method.Post)
                .AddBody(monitor);
            var resultAdd = await RestProxy.Invoke<int>(restRequestAdd);

            return new CliActionResponse(resultAdd, message: Convert.ToString(resultAdd.Data));
        }

        private static async Task<RestResponse<MonitorActionMedatada>> GetMetadata()
        {
            var restRequest = new RestRequest("monitor/metadata", Method.Get);
            var result = await RestProxy.Invoke<MonitorActionMedatada>(restRequest);
            return result;
        }

        private static string GetHook(RestResponse<MonitorActionMedatada> metadata)
        {
            var hooksTable = CliTableExtensions.GetTable(metadata.Data.Hooks, "Name");
            AnsiConsole.Write(hooksTable);
            var hookPrompt = new TextPrompt<int>("[turquoise2]  > Hook id: [/]")
                .Validate(hook =>
                {
                    if (metadata.Data.Hooks.ContainsKey(hook))
                    {
                        return ValidationResult.Success();
                    }

                    return ValidationResult.Error($"[red]hook id {hook} does not exist[/]");
                });

            var hookId = AnsiConsole.Prompt(hookPrompt);
            var hookName = metadata.Data.Hooks[hookId];
            return hookName;
        }

        private static int GetDistributionGroup(RestResponse<MonitorActionMedatada> metadata)
        {
            var groupsTable = CliTableExtensions.GetTable(metadata.Data.Groups, "Group Name");
            AnsiConsole.Write(groupsTable);

            var prompt = new TextPrompt<int>("[turquoise2]  > Distribution group id: [/]")
                .Validate(group =>
                {
                    if (metadata.Data.Groups.ContainsKey(group))
                    {
                        return ValidationResult.Success();
                    }

                    return ValidationResult.Error($"[red]group id {group} does not exist[/]");
                });

            var groupId = AnsiConsole.Prompt(prompt);
            return groupId;
        }

        private static string GetEventArguments(int monitorEvent)
        {
            return monitorEvent >= 10 ?
                AnsiConsole.Prompt(new TextPrompt<string>("[turquoise2]  > Monitor event argument: [/]").AllowEmpty()) :
                null;
        }

        private static int GetEvent(RestResponse<MonitorActionMedatada> metadata)
        {
            var evenTtable = CliTableExtensions.GetTable(metadata.Data.Events, "Event Name");
            AnsiConsole.Write(evenTtable);

            var prompt = new TextPrompt<int>("[turquoise2]  > Monitor event id: [/]")
                .Validate(ev =>
                {
                    if (metadata.Data.Events.ContainsKey(ev))
                    {
                        return ValidationResult.Success();
                    }

                    return ValidationResult.Error($"[red]event id {ev} does not exist[/]");
                });

            var monitorEvent = AnsiConsole.Prompt(prompt);
            return monitorEvent;
        }

        private static AddMonitorJobData GetJob(RestResponse<MonitorActionMedatada> metadata)
        {
            var result = new AddMonitorJobData();

            while (result.IsEmpty)
            {
                result.JobId = GetJobId(metadata);
                if (string.IsNullOrEmpty(result.JobId))
                {
                    result.JobGroupId = GetJobGroup(metadata);
                }

                if (result.IsEmpty)
                {
                    AnsiConsole.Write(new Markup("[red]job id and job group are empty[/]\r\n"));
                }
            }

            return result;
        }

        private static string GetJobId(RestResponse<MonitorActionMedatada> metadata)
        {
            var table = CliTableExtensions.GetTable(metadata.Data.Jobs, "Description");
            AnsiConsole.Write(table);

            var prompt = new TextPrompt<string>("[turquoise2]  > Job id: [/]")
                .AllowEmpty()
                .Validate(job =>
                {
                    if (string.IsNullOrEmpty(job))
                    {
                        return ValidationResult.Success();
                    }

                    job = job.Trim();
                    if (metadata.Data.Jobs.ContainsKey(job))
                    {
                        return ValidationResult.Success();
                    }

                    return ValidationResult.Error($"[red]job id {job} does not exist[/]");
                });

            var jobId = AnsiConsole.Prompt(prompt);
            if (string.IsNullOrEmpty(jobId))
            {
                jobId = null;
            }

            return jobId;
        }

        private static string GetJobGroup(RestResponse<MonitorActionMedatada> metadata)
        {
            string jobGroup = null;

            if (metadata.Data.JobGroups != null && metadata.Data.JobGroups.Count > 0)
            {
                var table = CliTableExtensions.GetTable(metadata.Data.JobGroups, "Job Group");
                AnsiConsole.Write(table);
            }

            jobGroup = AnsiConsole.Prompt(
                new TextPrompt<string>("[turquoise2]  > Job group: [/]")
                .AllowEmpty()
                .Validate(group =>
                {
                    if (string.IsNullOrEmpty(group))
                    {
                        return ValidationResult.Success();
                    }

                    group = group.Trim();
                    if (metadata.Data.JobGroups.Contains(group))
                    {
                        return ValidationResult.Success();
                    }

                    return ValidationResult.Error($"[red]group {group} does not exist[/]");
                }));

            if (string.IsNullOrEmpty(jobGroup))
            {
                jobGroup = null;
            }

            return jobGroup;
        }

        private static string GetTitle()
        {
            // === Title ===
            return AnsiConsole.Prompt(new TextPrompt<string>("[turquoise2]  > Title: [/]")
                .Validate(title =>
                {
                    if (string.IsNullOrWhiteSpace(title)) { return ValidationResult.Error("[red]Title is required field[/]"); }
                    title = title.Trim();
                    if (title.Length > 50) { return ValidationResult.Error("[red]Title limited to 50 chars maximum[/]"); }
                    if (title.Length < 5) { return ValidationResult.Error("[red]Title must have at least 5 chars[/]"); }
                    return ValidationResult.Success();
                }));
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
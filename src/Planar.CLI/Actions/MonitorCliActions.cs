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
                restRequest = new RestRequest("monitor/{key}", Method.Get)
                    .AddParameter("jobOrGroupId", request.JobIdOrJobGroup, ParameterType.UrlSegment);
            }

            var result = await RestProxy.Invoke<List<MonitorItem>>(restRequest);
            var table = CliTableExtensions.GetTable(result.Data);
            return new CliActionResponse(result, table);
        }

        [Action("update")]
        public static async Task<CliActionResponse> UpdateMonitor(CliUpdateEntityRequest request)
        {
            var restRequest = new RestRequest("monitor", Method.Patch)
                .AddBody(request);

            return await Execute(restRequest);
        }

        [Action("add")]
        [NullRequest]
        public static async Task<CliActionResponse> AddMonitorAction(CliAddMonitorRequest request)
        {
            request ??= await CollectAddMonitorRequestData();
            var mappedRequest = MapAddMonitorRequest(request);
            var restRequestAdd = new RestRequest("monitor", Method.Post)
                .AddBody(mappedRequest);
            var resultAdd = await RestProxy.Invoke<int>(restRequestAdd);

            return new CliActionResponse(resultAdd, message: Convert.ToString(resultAdd.Data));
        }

        private static AddMonitorRequest MapAddMonitorRequest(CliAddMonitorRequest request)
        {
            var result = JsonMapper.Map<AddMonitorRequest, CliAddMonitorRequest>(request);
            result.JobId = request.Id;
            return result;
        }

        private static async Task<CliAddMonitorRequest> CollectAddMonitorRequestData()
        {
            var eventsRequest = new RestRequest("monitor/events", Method.Get);
            var eventsTask = RestProxy.Invoke<List<LovItem>>(eventsRequest);

            var hooksRequest = new RestRequest("monitor/hooks", Method.Get);
            var hooksTask = RestProxy.Invoke<List<string>>(hooksRequest);

            var jobsRequest = new RestRequest("job", Method.Get)
                .AddQueryParameter("filter", (int)AllJobsMembers.AllUserJobs);
            var jobsTask = RestProxy.Invoke<List<JobRowDetails>>(jobsRequest);

            var groupsRequest = new RestRequest("group", Method.Get);
            var groupsTask = RestProxy.Invoke<List<GroupInfo>>(groupsRequest);

            var events = await eventsTask;
            if (events.IsSuccessful == false)
            {
            }

            var hooks = await hooksTask;
            if (hooks.IsSuccessful == false)
            {
            }

            var jobs = await jobsTask;
            if (jobs.IsSuccessful == false)
            {
            }

            var groups = await groupsTask;
            if (groups.IsSuccessful == false)
            {
            }

            if (!jobs.Data.Any())
            {
                throw new CliValidationException("there are no jobs for monitoring");
            }

            if (!hooks.Data.Any())
            {
                throw new CliValidationException("there are no monitor hooks define in service");
            }

            if (!groups.Data.Any())
            {
                throw new CliValidationException("there are no distribution groups define in service");
            }

            var title = GetTitle();
            var job = GetJob(jobs.Data);
            var monitorEvent = GetEvent(events.Data);
            var monitorEventArgs = GetEventArguments(monitorEvent);
            var groupId = GetDistributionGroup(groups.Data);
            var hookName = GetHook(hooks.Data);

            AnsiConsole.Write(new Rule());

            var monitor = new CliAddMonitorRequest
            {
                EventArgument = monitorEventArgs,
                JobGroup = job.JobGroupId,
                GroupId = groupId,
                Hook = hookName,
                Id = job.JobId,
                EventId = monitorEvent,
                Title = title
            };

            return monitor;
        }

        private static string GetHook(IEnumerable<string> hooks)
        {
            var selectedHook = AnsiConsole.Prompt(
                 new SelectionPrompt<string>()
                    .Title("[underline]select hook from the following list (press enter to select):[/]")
                    .PageSize(20)
                    .MoreChoicesText("[grey](Move up and down to reveal more hooks)[/]")
                    .AddChoices(hooks));

            AnsiConsole.MarkupLine($"[turquoise2]  > Hook: [/] {selectedHook}");

            return selectedHook;
        }

        private static int GetDistributionGroup(IEnumerable<GroupInfo> groups)
        {
            var groupsNames = groups.Select(group => group.Name);

            var selectedGroup = AnsiConsole.Prompt(
                 new SelectionPrompt<string>()
                    .Title("[underline]select distribution group from the following list (press enter to select):[/]")
                    .PageSize(20)
                    .MoreChoicesText("[grey](Move up and down to reveal more distribution group)[/]")
                    .AddChoices(groupsNames));

            var group = groups.Where(e => e.Name == selectedGroup).First();
            AnsiConsole.MarkupLine($"[turquoise2]  > Group: [/] {group}");
            return group.Id;
        }

        private static string GetEventArguments(int monitorEvent)
        {
            var result = monitorEvent >= 10 ?
                AnsiConsole.Prompt(new TextPrompt<string>("[turquoise2]  > Monitor event argument: [/]").AllowEmpty()) :
                null;

            if (!string.IsNullOrEmpty(result))
            {
                AnsiConsole.MarkupLine($"[turquoise2]  > Arguments: [/] {result}");
            }

            return result;
        }

        private static int GetEvent(IEnumerable<LovItem> events)
        {
            var eventsName = events.Select(e => e.Name);

            var selectedEvent = AnsiConsole.Prompt(
                 new SelectionPrompt<string>()
                     .Title("[underline]select monitor event from the following list (press enter to select):[/]")
                     .PageSize(20)
                     .MoreChoicesText("[grey](Move up and down to reveal more monitor event)[/]")
                     .AddChoices(eventsName));

            var monitorEvent = events.Where(e => e.Name == selectedEvent).First();
            AnsiConsole.MarkupLine($"[turquoise2]  > Event: [/] {monitorEvent}");
            return monitorEvent.Id;
        }

        private static AddMonitorJobData GetJob(IEnumerable<JobRowDetails> jobs)
        {
            var type = new[] { "single (monitor for single job)", "group (monitor for group of jobs)", "all (monitor for all jobs)" };

            var selectedEvent = AnsiConsole.Prompt(
                 new SelectionPrompt<string>()
                    .Title("[underline]which kind of monitor do you want to create? (press enter to select):[/]")
                    .PageSize(20)
                    .MoreChoicesText("[grey](Move up and down to reveal more monitor event)[/]")
                    .AddChoices(type));

            selectedEvent = selectedEvent.Split(' ').First();

            if (selectedEvent == "all")
            {
                AnsiConsole.MarkupLine("[turquoise2]  > Monitor for: [/] all jobs");
                return new AddMonitorJobData();
            }

            if (selectedEvent == "group")
            {
                var group = JobCliActions.ChooseGroup(jobs);
                AnsiConsole.MarkupLine($"[turquoise2]  > Monitor for: [/] job group '{group}'");
                return new AddMonitorJobData { JobGroupId = group };
            }

            var job = JobCliActions.ChooseJob(jobs);
            AnsiConsole.MarkupLine($"[turquoise2]  > Monitor for: [/] single job '{job}'");
            return new AddMonitorJobData { JobId = job };
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
            var result = await RestProxy.Invoke<List<LovItem>>(restRequest);
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
using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using Planar.CLI.General;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("monitor")]
    public class MonitorCliActions : BaseCliAction<MonitorCliActions>
    {
        [Action("add")]
        [NullRequest]
        public static async Task<CliActionResponse> AddMonitorAction(CliAddMonitorRequest request)
        {
            if (request == null)
            {
                var wrapper = await CollectAddMonitorRequestData();
                if (!wrapper.IsSuccessful)
                {
                    return new CliActionResponse(wrapper.FailResponse);
                }

                request = wrapper.Request;
            }

            var mappedRequest = MapAddMonitorRequest(request);
            var restRequestAdd = new RestRequest("monitor", Method.Post)
                .AddBody(mappedRequest);
            var resultAdd = await RestProxy.Invoke<EntityIdResponse>(restRequestAdd);

            return new CliActionResponse(resultAdd, message: Convert.ToString(resultAdd.Data?.Id));
        }

        [Action("remove")]
        [Action("delete")]
        public static async Task<CliActionResponse> DeleteMonitor(CliGetByIdRequest request)
        {
            if (!ConfirmAction($"remove monitor id {request.Id}")) { return CliActionResponse.Empty; }

            var restRequest = new RestRequest("monitor/{id}", Method.Delete)
                .AddParameter("id", request.Id, ParameterType.UrlSegment);
            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("ls")]
        public static async Task<CliActionResponse> GetMonitorActions(CliGetMonitorActionsRequest request)
        {
            var data = new List<MonitorItem>();
            RestResponse finalResult;

            if (string.IsNullOrEmpty(request.JobIdOrJobGroup))
            {
                var restRequest = new RestRequest("monitor", Method.Get);
                var result = await RestProxy.Invoke<List<MonitorItem>>(restRequest);
                if (result.IsSuccessful && result.Data != null) { data.AddRange(result.Data); }
                finalResult = result;
            }
            else
            {
                var restRequest1 = new RestRequest("monitor/byJob/{jobId}", Method.Get)
                    .AddParameter("jobId", request.JobIdOrJobGroup, ParameterType.UrlSegment);

                var restRequest2 = new RestRequest("monitor/byGroup/{group}", Method.Get)
                    .AddParameter("group", request.JobIdOrJobGroup, ParameterType.UrlSegment);

                var task1 = RestProxy.Invoke<List<MonitorItem>>(restRequest1);
                var task2 = RestProxy.Invoke<List<MonitorItem>>(restRequest2);
                await Task.WhenAll(task1, task2);

                var result1 = task1.Result;
                var result2 = task2.Result;
                if (result1.IsSuccessful && result1.Data != null) { data.AddRange(result1.Data); }
                if (result2.IsSuccessful && result2.Data != null) { data.AddRange(result2.Data); }

                finalResult = SelectRestResponse(result1, result2);
            }

            var table = CliTableExtensions.GetTable(data);
            return new CliActionResponse(finalResult, table);
        }

        [Action("events")]
        public static async Task<CliActionResponse> GetMonitorEvents()
        {
            var restRequest = new RestRequest("monitor/events", Method.Get);
            var result = await RestProxy.Invoke<List<LovItem>>(restRequest);
            var table = CliTableExtensions.GetTable(result.Data);
            return new CliActionResponse(result, table);
        }

        [Action("hooks")]
        public static async Task<CliActionResponse> GetMonitorHooks()
        {
            var restRequest = new RestRequest("monitor/hooks", Method.Get);
            var result = await RestProxy.Invoke<List<string>>(restRequest);
            return new CliActionResponse(result, serializeObj: result.Data);
        }

        [Action("reload")]
        public static async Task<CliActionResponse> ReloadMonitor()
        {
            var restRequest = new RestRequest("monitor/reload", Method.Post);
            var result = await RestProxy.Invoke<string>(restRequest);
            return new CliActionResponse(result, message: result.Data);
        }

        [Action("update")]
        public static async Task<CliActionResponse> UpdateMonitor(CliUpdateEntityRequest request)
        {
            var restRequest = new RestRequest("monitor", Method.Patch)
                .AddBody(request);

            return await Execute(restRequest);
        }

        private static async Task<RequestBuilderWrapper<CliAddMonitorRequest>> CollectAddMonitorRequestData()
        {
            var data = await GetMonitorData();
            if (!data.IsSuccessful)
            {
                return new RequestBuilderWrapper<CliAddMonitorRequest> { FailResponse = data.FailResponse };
            }

            var monitorEventId = GetEvent(data.Events);
            var job = GetJob(data.Jobs, monitorEventId);
            var monitorEventArgs = GetEventArguments(monitorEventId);
            var groupId = GetDistributionGroup(data.Groups);
            var hookName = GetHook(data.Hooks);
            var title = GetTitle();

            AnsiConsole.Write(new Rule());

            var monitor = new CliAddMonitorRequest
            {
                EventArgument = monitorEventArgs,
                JobGroup = job.JobGroup,
                GroupId = groupId,
                Hook = hookName,
                JobName = job.JobName,
                EventId = monitorEventId,
                Title = title
            };

            return new RequestBuilderWrapper<CliAddMonitorRequest> { Request = monitor };
        }

        private static int GetDistributionGroup(IEnumerable<GroupInfo> groups)
        {
            var groupsNames = groups.Select(group => group.Name);
            var selectedGroup = PromptSelection(groupsNames, "distribution group");

            var group = groups.First(e => e.Name == selectedGroup);
            AnsiConsole.MarkupLine($"[turquoise2]  > Group: [/] {group.Name}");
            return group.Id;
        }

        private static int GetEvent(IEnumerable<LovItem> events)
        {
            var eventsName = events.Select(e => e.Name);

            var selectedEvent = PromptSelection(eventsName, "monitor event");

            var monitorEvent = events.First(e => e.Name == selectedEvent);
            AnsiConsole.MarkupLine($"[turquoise2]  > Event: [/] {monitorEvent.Name}");
            return monitorEvent.Id;
        }

        private static string? GetEventArguments(int monitorEvent)
        {
            var result = MonitorEventsExtensions.IsMonitorEventHasArguments(monitorEvent) ?
                AnsiConsole.Prompt(new TextPrompt<string>("[turquoise2]  > Event argument: [/]").AllowEmpty()) :
                null;

            if (!string.IsNullOrEmpty(result))
            {
                AnsiConsole.MarkupLine($"[turquoise2]  > Arguments: [/] {result}");
            }

            return result;
        }

        private static string GetHook(IEnumerable<string> hooks)
        {
            var selectedHook = PromptSelection(hooks, "hook");

            AnsiConsole.MarkupLine($"[turquoise2]  > Hook: [/] {selectedHook}");

            return selectedHook ?? string.Empty;
        }

        private static AddMonitorJobData GetJob(IEnumerable<JobRowDetails> jobs, int monitorEventId)
        {
            if (MonitorEventsExtensions.IsSystemMonitorEvent(monitorEventId))
            {
                return new AddMonitorJobData();
            }

            var types = new[] { "single (monitor for single job)", "group (monitor for group of jobs)", "all (monitor for all jobs)" };

            var selectedEvent = PromptSelection(types, "monitor type") ?? string.Empty;

            selectedEvent = selectedEvent.Split(' ').First();

            if (selectedEvent == "all")
            {
                AnsiConsole.MarkupLine("[turquoise2]  > Monitor for: [/] all jobs");
                return new AddMonitorJobData();
            }

            if (selectedEvent == "group")
            {
                var group = JobCliActions.ChooseGroup(jobs) ?? string.Empty;
                AnsiConsole.MarkupLine($"[turquoise2]  > Monitor for: [/] job group '{group}'");
                return new AddMonitorJobData { JobGroup = group };
            }

            var job = JobCliActions.ChooseJob(jobs);
            AnsiConsole.MarkupLine($"[turquoise2]  > Monitor for: [/] single job '{job}'");
            var key = JobKey.Parse(job);
            return new AddMonitorJobData { JobName = key.Name, JobGroup = key.Group };
        }

        private static async Task<MonitorRequestData> GetMonitorData()
        {
            var data = new MonitorRequestData();
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
            data.Events = events.Data ?? new List<LovItem>();
            if (!events.IsSuccessful)
            {
                data.FailResponse = events;
                return data;
            }

            var hooks = await hooksTask;
            data.Hooks = hooks.Data ?? new List<string>();
            if (!hooks.IsSuccessful)
            {
                data.FailResponse = hooks;
                return data;
            }

            var jobs = await jobsTask;
            data.Jobs = jobs.Data ?? new List<JobRowDetails>();
            if (!jobs.IsSuccessful)
            {
                data.FailResponse = jobs;
                return data;
            }

            var groups = await groupsTask;
            data.Groups = groups.Data ?? new List<GroupInfo>();
            if (!groups.IsSuccessful)
            {
                data.FailResponse = groups;
                return data;
            }

            if (!data.Jobs.Any())
            {
                throw new CliWarningException("there are no jobs for monitoring");
            }

            if (!data.Hooks.Any())
            {
                throw new CliWarningException("there are no monitor hooks define in service");
            }

            if (!data.Groups.Any())
            {
                throw new CliWarningException("there are no distribution groups define in service");
            }

            return data;
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

        private static AddMonitorRequest MapAddMonitorRequest(CliAddMonitorRequest request)
        {
            var result = JsonMapper.Map<AddMonitorRequest, CliAddMonitorRequest>(request);
            return result ?? new AddMonitorRequest();
        }

        private static RestResponse SelectRestResponse(params RestResponse[] items)
        {
            RestResponse? result = null;
            if (items.Any())
            {
                result = items.FirstOrDefault(i => !i.IsSuccessful && (int)i.StatusCode >= 500);
            }

            result ??= CliActionResponse.GetGenericSuccessRestResponse();
            return result;
        }

        private struct MonitorRequestData
        {
            public List<LovItem> Events { get; set; }
            public List<GroupInfo> Groups { get; set; }
            public List<string> Hooks { get; set; }
            public List<JobRowDetails> Jobs { get; set; }
            public RestResponse FailResponse { get; set; }
            public bool IsSuccessful => FailResponse == null;
        }

        private struct AddMonitorJobData
        {
            public string JobGroup { get; set; }
            public string JobName { get; set; }
        }
    }
}
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.CliGeneral;
using Planar.CLI.Entities;
using Planar.CLI.General;
using Planar.CLI.Proxy;
using Planar.Common;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("monitor", "Actions to handle monitoring and monitor hooks", Synonyms = "monitors")]
    public class MonitorCliActions : BaseCliAction<MonitorCliActions>
    {
        [Action("add")]
        [NullRequest]
        [ActionWizard]
        public static async Task<CliActionResponse> AddMonitorAction(CliAddMonitorRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                var wrapper = await CollectAddMonitorRequestData(cancellationToken);
                if (!wrapper.IsSuccessful || wrapper.Request == null)
                {
                    return new CliActionResponse(wrapper.FailResponse);
                }

                request = wrapper.Request;
            }

            var mappedRequest = MapAddMonitorRequest(request);
            var restRequestAdd = new RestRequest("monitor", Method.Post)
                .AddBody(mappedRequest);
            var resultAdd = await RestProxy.Invoke<EntityIdResponse>(restRequestAdd, cancellationToken);
            return new CliActionResponse(resultAdd);
        }

        [Action("remove")]
        [Action("delete")]
        public static async Task<CliActionResponse> DeleteMonitor(CliGetByIdRequest request, CancellationToken cancellationToken = default)
        {
            if (!ConfirmAction($"remove monitor id {request.Id}")) { return CliActionResponse.Empty; }

            var restRequest = new RestRequest("monitor/{id}", Method.Delete)
                .AddParameter("id", request.Id, ParameterType.UrlSegment);
            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("get")]
        public static async Task<CliActionResponse> GetMonitoActions(CliGetByIdRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("monitor/{id}", Method.Get)
                .AddParameter("id", request.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<MonitorItem>(restRequest, cancellationToken);
            return new CliActionResponse(result, dumpObject: result.Data);
        }

        [Action("ls")]
        [Action("list")]
        public static async Task<CliActionResponse> GetMonitorActions(CliGetMonitorActionsRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(request.JobIdOrJobGroup))
            {
                var restRequest = new RestRequest("monitor", Method.Get)
                    .AddQueryPagingParameter(request);

                var result = await RestProxy.Invoke<PagingResponse<MonitorItem>>(restRequest, cancellationToken);

                var table = CliTableExtensions.GetTable(result?.Data);
                return new CliActionResponse(result, table);
            }
            else
            {
                var restRequest1 = new RestRequest("monitor/by-job/{jobId}", Method.Get)
                    .AddParameter("jobId", request.JobIdOrJobGroup, ParameterType.UrlSegment);

                var restRequest2 = new RestRequest("monitor/by-group/{group}", Method.Get)
                    .AddParameter("group", request.JobIdOrJobGroup, ParameterType.UrlSegment);

                var task1 = RestProxy.Invoke<List<MonitorItem>>(restRequest1, cancellationToken);
                var task2 = RestProxy.Invoke<List<MonitorItem>>(restRequest2, cancellationToken);
                await Task.WhenAll(task1, task2);

                var result1 = task1.Result;
                var result2 = task2.Result;
                var data = new List<MonitorItem>();
                if (result1.IsSuccessful && result1.Data != null) { data.AddRange(result1.Data); }
                if (result2.IsSuccessful && result2.Data != null) { data.AddRange(result2.Data); }
                var total = data.Count;
                data = data.SetPaging(request).ToList();

                var pagingResponse = new PagingResponse();
                pagingResponse.SetPagingData(request, total);
                var final = new PagingResponse<MonitorItem>(request, data, total);
                var result = SelectRestResponse(result1, result2);
                result.Content = JsonConvert.SerializeObject(final);
                var table = CliTableExtensions.GetTable(final);
                return new CliActionResponse(result, table);
            }
        }

        [Action("events")]
        public static async Task<CliActionResponse> GetMonitorEvents(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("monitor/events", Method.Get);
            var result = await RestProxy.Invoke<List<MonitorEventModel>>(restRequest, cancellationToken);
            var table = CliTableExtensions.GetTable(result.Data);
            return new CliActionResponse(result, table);
        }

        [Action("hooks")]
        public static async Task<CliActionResponse> GetMonitorHooks(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("monitor/hooks", Method.Get);
            var result = await RestProxy.Invoke<List<string>>(restRequest, cancellationToken);
            return new CliActionResponse(result, dumpObject: result.Data);
        }

        [Action("reload")]
        public static async Task<CliActionResponse> ReloadMonitor(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("monitor/reload", Method.Post);
            var result = await RestProxy.Invoke<string>(restRequest, cancellationToken);
            return new CliActionResponse(result, message: result.Data);
        }

        [Action("update")]
        public static async Task<CliActionResponse> UpdateMonitor(CliUpdateEntityByIdRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("monitor", Method.Patch)
                .AddBody(request);

            return await Execute(restRequest, cancellationToken);
        }

        [Action("get-alert")]
        public static async Task<CliActionResponse> GetAlert(CliGetByIdRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("monitor/alert/{id}", Method.Get)
                .AddUrlSegment("id", request.Id);

            var result = await RestProxy.Invoke<MonitorAlertModel>(restRequest, cancellationToken);
            return new CliActionResponse(result, dumpObject: result.Data);
        }

        [Action("alerts")]
        public static async Task<CliActionResponse> ListAlerts(CliGetMonitorsAlertsRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("monitor/alerts", Method.Get)
                .AddEntityToQueryParameter(request);

            var result = await RestProxy.Invoke<PagingResponse<MonitorAlertRowModel>>(restRequest, cancellationToken);
            var table = CliTableExtensions.GetTable(result.Data);
            return new CliActionResponse(result, table);
        }

        [Action("try")]
        [ActionWizard]
        [NullRequest]
        public static async Task<CliActionResponse> TryMonitor(CliMonitorTestRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                var wrapper = await CollectTestMonitorRequestData(cancellationToken);
                if (!wrapper.IsSuccessful || wrapper.Request == null)
                {
                    return new CliActionResponse(wrapper.FailResponse);
                }

                request = wrapper.Request;
            }

            var restRequest = new RestRequest("monitor/try", Method.Post)
                .AddBody(request);

            restRequest.Timeout = 120000;
            AnsiConsole.MarkupLine($"[grey62]  > (please wait... this action may take up to 2 minutes)[/]");
            return await Execute(restRequest, cancellationToken);
        }

        [Action("mute")]
        [NullRequest]
        public static async Task<CliActionResponse> MuteMonitor(CliMonitorMuteRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                request = new CliMonitorMuteRequest();
                var wrapper = await CollectCliMonitorMuteRequest(request, cancellationToken);
                if (!wrapper.IsSuccessful)
                {
                    return new CliActionResponse(wrapper.FailResponse);
                }
            }

            if (!ConfirmAction($"mute monitor (see details above)")) { return CliActionResponse.Empty; }

            var restRequest = new RestRequest("monitor/mute", Method.Patch)
                .AddBody(new
                {
                    request.JobId,
                    request.MonitorId,
                    DueDate = DateTime.Now.Add(request.TimeSpan)
                });

            return await Execute(restRequest, cancellationToken);
        }

        [Action("unmute")]
        [NullRequest]
        public static async Task<CliActionResponse> UnmuteMonitor(CliMonitorUnmuteRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                request = new CliMonitorMuteRequest();
                var wrapper = await CollectCliMonitorUnmuteRequest(request, cancellationToken);
                if (!wrapper.IsSuccessful)
                {
                    return new CliActionResponse(wrapper.FailResponse);
                }
            }

            var restRequest = new RestRequest("monitor/unmute", Method.Patch)
                .AddBody(request);

            return await Execute(restRequest, cancellationToken);
        }

        [Action("mutes")]
        public static async Task<CliActionResponse> Mutes(CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("monitor/mutes", Method.Get);
            var result = await RestProxy.Invoke<List<MuteItem>>(restRequest, cancellationToken);
            var table = CliTableExtensions.GetTable(result.Data);
            return new CliActionResponse(result, table);
        }

        private static async Task<RequestBuilderWrapper<CliMonitorTestRequest>> CollectTestMonitorRequestData(CancellationToken cancellationToken = default)
        {
            var data = await GetTestMonitorData(cancellationToken);
            if (!data.IsSuccessful)
            {
                return new RequestBuilderWrapper<CliMonitorTestRequest> { FailResponse = data.FailResponse };
            }

            var hookName = GetHook(data.Hooks);
            var eventName = GetEventForTest();
            var groupName = GetDistributionGroup(data.Groups);

            var monitor = new CliMonitorTestRequest
            {
                GroupName = groupName,
                Hook = hookName,
                EventName = eventName
            };

            return new RequestBuilderWrapper<CliMonitorTestRequest> { Request = monitor };
        }

        private static async Task<CliPromptWrapper> CollectCliMonitorMuteRequest(CliMonitorMuteRequest request, CancellationToken cancellationToken)
        {
            var wrapper = await CollectCliMonitorUnmuteRequest(request, cancellationToken);
            if (!wrapper.IsSuccessful)
            {
                return wrapper;
            }

            if (request.TimeSpan == TimeSpan.Zero)
            {
                var ts = CliPromptUtil.PromptForTimeSpan("mute duration", required: true);
                request.TimeSpan = ts ?? TimeSpan.Zero;
            }

            return CliPromptWrapper.Success;
        }

        private static async Task<bool> IsMonitorIsSystem(int monitorId, CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("monitor/{id}", Method.Get)
                .AddParameter("id", monitorId, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<MonitorItem>(restRequest, cancellationToken);
            if (!result.IsSuccessful || result.Data == null) { return false; }

            return MonitorEventsExtensions.IsSystemMonitorEvent(result.Data.EventId);
        }

        private static async Task<CliPromptWrapper> CollectCliMonitorUnmuteRequest(CliMonitorUnmuteRequest request, CancellationToken cancellationToken)
        {
            if (request.MonitorId == null)
            {
                const string opt1 = "all monitors";
                const string opt2 = "specific monitor";

                var opt = CliPromptUtil.PromptSelection(new[] { opt2, opt1 }, "monitor/s to mute");
                if (opt == opt1)
                {
                    AnsiConsole.MarkupLine($"[turquoise2]  > {opt1}[/]");
                }
                else
                {
                    var monitorWrapper = await CliPromptUtil.Monitors(cancellationToken);
                    if (!monitorWrapper.IsSuccessful)
                    {
                        return monitorWrapper;
                    }

                    request.MonitorId = monitorWrapper.Value?.Id;
                    AnsiConsole.MarkupLine($"[turquoise2]  > monitor :[/] {monitorWrapper.Value?.Id} - {monitorWrapper.Value?.EventTitle}");
                }
            }

            if (string.IsNullOrEmpty(request.JobId))
            {
                const string opt1 = "all jobs";
                const string opt2 = "specific job";

                if (request.MonitorId != null && await IsMonitorIsSystem(request.MonitorId.Value, cancellationToken))
                {
                    AnsiConsole.MarkupLine($"[turquoise2]  > {opt1}[/]");
                    return CliPromptWrapper.Success;
                }

                var opt = CliPromptUtil.PromptSelection(new[] { opt2, opt1 }, "job/s to mute");
                if (opt == opt1)
                {
                    AnsiConsole.MarkupLine($"[turquoise2]  > {opt1}[/]");
                }
                else
                {
                    request.JobId = await JobCliActions.ChooseJob(cancellationToken);
                    AnsiConsole.MarkupLine($"[turquoise2]  > job id :[/] {request.JobId}");
                }
            }

            return CliPromptWrapper.Success;
        }

        private static async Task<RequestBuilderWrapper<CliAddMonitorRequest>> CollectAddMonitorRequestData(CancellationToken cancellationToken)
        {
            var data = await GetMonitorData(cancellationToken);
            if (!data.IsSuccessful)
            {
                return new RequestBuilderWrapper<CliAddMonitorRequest> { FailResponse = data.FailResponse };
            }

            var eventName = GetEvent(data.Events);
            var job = GetJob(data.Jobs, eventName);
            var monitorEventArgs = GetEventArguments(eventName);
            var groupName = GetDistributionGroup(data.Groups);
            var hookName = GetHook(data.Hooks);
            var title = GetTitle();

            var monitor = new CliAddMonitorRequest
            {
                EventArgument = monitorEventArgs,
                JobGroup = job.JobGroup,
                GroupName = groupName,
                Hook = hookName,
                JobName = job.JobName,
                EventName = eventName,
                Title = title
            };

            return new RequestBuilderWrapper<CliAddMonitorRequest> { Request = monitor };
        }

        private static string GetDistributionGroup(IEnumerable<GroupInfo> groups)
        {
            var groupsNames = groups.Select(group => group.Name ?? string.Empty);
            var selectedGroup = PromptSelection(groupsNames, "distribution group");

            AnsiConsole.MarkupLine($"[turquoise2]  > dist. group:[/] {selectedGroup}");
            return selectedGroup ?? string.Empty;
        }

        private static string GetEvent(IEnumerable<MonitorEventModel> events)
        {
            var eventsName = events.Select(e => e.EventTitle);
            var selectedEvent = PromptSelection(eventsName, "monitor event");
            AnsiConsole.MarkupLine($"[turquoise2]  > event:[/] {selectedEvent}");
            var result = events.FirstOrDefault(e => e.EventTitle == selectedEvent)?.EventName;
            return result ?? string.Empty;
        }

        private static string GetEventForTest()
        {
            var names = Enum.GetNames(typeof(TestMonitorEvents));
            var selectedEvent = PromptSelection(names, "monitor event");

            if (string.IsNullOrEmpty(selectedEvent))
            {
                throw new CliWarningException("monitor event is not selected");
            }

            AnsiConsole.MarkupLine($"[turquoise2]  > event:[/] {selectedEvent}");
            return selectedEvent;
        }

        private static string? GetEventArguments(string eventName)
        {
            var result = MonitorEventsExtensions.IsMonitorEventHasArguments(eventName) ?
                AnsiConsole.Prompt(new TextPrompt<string>("[turquoise2]  > event argument:[/]").AllowEmpty()) :
                null;

            if (!string.IsNullOrEmpty(result))
            {
                AnsiConsole.MarkupLine($"[turquoise2]  > arguments:[/] {result}");
            }

            return result;
        }

        private static string GetHook(IEnumerable<string> hooks)
        {
            var selectedHook = PromptSelection(hooks, "hook");

            AnsiConsole.MarkupLine($"[turquoise2]  > hook: [/] {selectedHook}");

            return selectedHook ?? string.Empty;
        }

        private static AddMonitorJobData GetJob(IEnumerable<JobBasicDetails> jobs, string eventName)
        {
            if (MonitorEventsExtensions.IsSystemMonitorEvent(eventName))
            {
                return new AddMonitorJobData();
            }

            string selectedEvent;
            if (MonitorEventsExtensions.IsMonitorEventHasArguments(eventName))
            {
                selectedEvent = "single (monitor for single job)";
            }
            else
            {
                var types = new[] { "single (monitor for single job)", "group (monitor for group of jobs)", "all (monitor for all jobs)" };
                selectedEvent = PromptSelection(types, "monitor type") ?? string.Empty;
            }

            selectedEvent = selectedEvent.Split(' ')[0];
            if (selectedEvent == "all")
            {
                AnsiConsole.MarkupLine("[turquoise2]  > monitor for: [/] all jobs");
                return new AddMonitorJobData();
            }

            if (selectedEvent == "group")
            {
                var group = JobCliActions.ChooseGroup(jobs) ?? string.Empty;
                AnsiConsole.MarkupLine($"[turquoise2]  > monitor for:[/] job group '{group}'");
                return new AddMonitorJobData { JobGroup = group };
            }

            var job = JobCliActions.ChooseJob(jobs);
            AnsiConsole.MarkupLine($"[turquoise2]  > monitor for:[/] single job '{job}'");
            var key = JobKey.Parse(job);
            return new AddMonitorJobData { JobName = key.Name, JobGroup = key.Group };
        }

        private static async Task<TestMonitorData> GetTestMonitorData(CancellationToken cancellationToken)
        {
            var data = new TestMonitorData();
            var hooksRequest = new RestRequest("monitor/hooks", Method.Get);
            var hooksTask = RestProxy.Invoke<List<string>>(hooksRequest, cancellationToken);

            var groupsRequest = new RestRequest("group", Method.Get)
                .AddQueryPagingParameter(1000);
            var groupsTask = RestProxy.Invoke<PagingResponse<GroupInfo>>(groupsRequest, cancellationToken);

            var hooks = await hooksTask;
            data.Hooks = hooks.Data ?? new List<string>();
            if (!hooks.IsSuccessful)
            {
                data.FailResponse = hooks;
                return data;
            }

            var groups = await groupsTask;
            data.Groups = groups.Data?.Data ?? new List<GroupInfo>();
            if (!groups.IsSuccessful)
            {
                data.FailResponse = groups;
                return data;
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

        private static async Task<MonitorRequestData> GetMonitorData(CancellationToken cancellationToken)
        {
            var data = new MonitorRequestData();
            var eventsRequest = new RestRequest("monitor/events", Method.Get);
            var eventsTask = RestProxy.Invoke<List<MonitorEventModel>>(eventsRequest, cancellationToken);

            var hooksRequest = new RestRequest("monitor/hooks", Method.Get);
            var hooksTask = RestProxy.Invoke<List<string>>(hooksRequest, cancellationToken);

            var jobsRequest = new RestRequest("job", Method.Get)
                .AddQueryParameter("filter", (int)AllJobsMembers.AllUserJobs)
                .AddQueryPagingParameter(1000);
            var jobsTask = RestProxy.Invoke<PagingResponse<JobBasicDetails>>(jobsRequest, cancellationToken);

            var groupsRequest = new RestRequest("group", Method.Get)
                .AddQueryPagingParameter(1000);
            var groupsTask = RestProxy.Invoke<PagingResponse<GroupInfo>>(groupsRequest, cancellationToken);

            var events = await eventsTask;
            data.Events = events.Data ?? new List<MonitorEventModel>();
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
            data.Jobs = jobs.Data?.Data ?? new List<JobBasicDetails>();
            if (!jobs.IsSuccessful)
            {
                data.FailResponse = jobs;
                return data;
            }

            var groups = await groupsTask;
            data.Groups = groups.Data?.Data ?? new List<GroupInfo>();
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
            var title = AnsiConsole.Prompt(new TextPrompt<string>("[turquoise2]  > title:[/]")
                .Validate(title =>
                {
                    if (string.IsNullOrWhiteSpace(title)) { return ValidationResult.Error("[red]title is required field[/]"); }
                    title = title.Trim();
                    if (title.Length > 50) { return ValidationResult.Error("[red]title limited to 50 chars maximum[/]"); }
                    if (title.Length < 5) { return ValidationResult.Error("[red]title must have at least 5 chars[/]"); }
                    return ValidationResult.Success();
                }));

            return title;
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
                result = Array.Find(items, i => !i.IsSuccessful && (int)i.StatusCode >= 500);
            }

            result ??= CliActionResponse.GetGenericSuccessRestResponse();
            return result;
        }

        private struct TestMonitorData
        {
            public List<GroupInfo> Groups { get; set; }
            public List<string> Hooks { get; set; }
            public RestResponse FailResponse { get; set; }
            public readonly bool IsSuccessful => FailResponse == null;
        }

        private struct MonitorRequestData
        {
            public List<MonitorEventModel> Events { get; set; }
            public List<GroupInfo> Groups { get; set; }
            public List<string> Hooks { get; set; }
            public List<JobBasicDetails> Jobs { get; set; }
            public RestResponse FailResponse { get; set; }
            public readonly bool IsSuccessful => FailResponse == null;
        }

        private struct AddMonitorJobData
        {
            public string JobGroup { get; set; }
            public string JobName { get; set; }
        }
    }
}
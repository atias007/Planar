using FluentValidation;
using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using Planar.CLI.Exceptions;
using Planar.CLI.General;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("job")]
    public class JobCliActions : BaseCliAction<JobCliActions>
    {
        [Action("add")]
        [NullRequest]
        public static async Task<CliActionResponse> AddJob(CliAddJobRequest request)
        {
            if (request == null)
            {
                var wrapper = await GetCliAddJobRequest();
                if (!wrapper.IsSuccessful)
                {
                    return new CliActionResponse(wrapper.FailResponse);
                }

                request = wrapper.Request;
            }

            var body = new SetJobFoldeRequest { Folder = request.Folder };
            var restRequest = new RestRequest("job/folder", Method.Post)
                .AddBody(body);
            var result = await RestProxy.Invoke<JobIdResponse>(restRequest);

            AssertCreated(result);
            return new CliActionResponse(result);
        }

        private static async Task<RequestBuilderWrapper<CliAddJobRequest>> GetCliAddJobRequest()
        {
            var restRequest = new RestRequest("job/available-jobs", Method.Get);
            var result = await RestProxy.Invoke<List<AvailableJobToAdd>>(restRequest);
            if (!result.IsSuccessful)
            {
                return new RequestBuilderWrapper<CliAddJobRequest> { FailResponse = result };
            }

            var folder = SelectJobFolder(result.Data);
            var request = new CliAddJobRequest { Folder = folder };
            return new RequestBuilderWrapper<CliAddJobRequest> { Request = request };
        }

        private static string SelectJobFolder(IEnumerable<AvailableJobToAdd> data)
        {
            if (!data.Any())
            {
                throw new CliWarningException("no available jobs found on server");
            }

            var folders = data.Select(e =>
                e.Name == e.RelativeFolder ?
                e.Name :
                $"{e.Name} ({e.RelativeFolder})");

            var selectedItem = PromptSelection(folders, "job folder");
            const string template = @"\(([^)]+)\)";
            var regex = new Regex(template, RegexOptions.None, TimeSpan.FromMilliseconds(500));
            var matches = regex.Matches(selectedItem);

            var selectedFolder = matches.LastOrDefault();
            return selectedFolder == null ? selectedItem : selectedFolder.Value[1..^1];
        }

        [Action("update")]
        public static async Task<CliActionResponse> UpdateJob(CliUpdateJobRequest request)
        {
            UpdateJobOptions options;

            if (request.Options == null)
            {
                options = MapUpdateJobOptions();
            }
            else
            {
                options = MapUpdateJobOptions(request.Options);
            }

            var body = new UpdateJobFolderRequest { Folder = request.Folder, UpdateJobOptions = options };
            var restRequest = new RestRequest("job/folder", Method.Put)
                .AddBody(body);

            var result = await RestProxy.Invoke<JobIdResponse>(restRequest);
            AssertJobUpdated(result);
            return new CliActionResponse(result);
        }

        private static UpdateJobOptions MapUpdateJobOptions()
        {
            var options = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("select update options:")
                    .Required() // Not required to have a favorite fruit
                    .InstructionsText(
                        "[grey](Press [blue]<space>[/] to toggle a choise, [green]<enter>[/] to accept)[/]")
                    .PageSize(15)
                    .AddChoiceGroup("all", new[] { "job details", "job data", "properties", "triggers", "triggers data" })
                    .AddChoiceGroup("all job", new[] { "job details", "job data", "properties" })
                    .AddChoiceGroup("all triggers", new[] { "triggers", "triggers data" })
                    );

            var result = MapUpdateJobOptions(options);
            return result;
        }

        private static UpdateJobOptions MapUpdateJobOptions(string options)
        {
            var items = options.Split(',');
            var result = MapUpdateJobOptions(items);
            return result;
        }

        private static UpdateJobOptions MapUpdateJobOptions(IEnumerable<string> items)
        {
            var result = new UpdateJobOptions();

            foreach (var item in items)
            {
                switch (item.ToLower())
                {
                    case "all":
                        result.UpdateJobDetails = true;
                        result.UpdateJobData = true;
                        result.UpdateProperties = true;
                        result.UpdateTriggers = true;
                        result.UpdateTriggersData = true;
                        break;

                    case "all job":
                    case "all-job":
                        result.UpdateJobDetails = true;
                        result.UpdateJobData = true;
                        result.UpdateProperties = true;
                        break;

                    case "all triggers":
                    case "all-triggers":
                        result.UpdateTriggers = true;
                        result.UpdateTriggersData = true;
                        break;

                    case "job":
                    case "job details":
                        result.UpdateJobDetails = true;
                        break;

                    case "job-data":
                    case "job data":
                        result.UpdateJobData = true;
                        break;

                    case "properties":
                        result.UpdateProperties = true;
                        break;

                    case "triggers":
                        result.UpdateTriggers = true;
                        break;

                    case "triggers-data":
                    case "triggers data":
                        result.UpdateTriggersData = true;
                        break;

                    default:
                        throw new ValidationException($"option {item} is invalid. use one or more from the following options: all,all-job,all-trigger,job,job-data,properties,triggers,triggers-data");
                }
            }

            return result;
        }

        [Action("ls")]
        [Action("list")]
        public static async Task<CliActionResponse> GetAllJobs(CliGetAllJobsRequest request)
        {
            var restRequest = new RestRequest("job", Method.Get);
            var p = AllJobsMembers.AllUserJobs;
            if (request.System) { p = AllJobsMembers.AllSystemJobs; }
            if (request.All) { p = AllJobsMembers.All; }
            restRequest.AddQueryParameter("filter", (int)p);

            var result = await RestProxy.Invoke<List<JobRowDetails>>(restRequest);
            var message = string.Empty;
            CliActionResponse response;
            if (request.Quiet)
            {
                message = string.Join('\n', result.Data?.Select(r => r.Id));
                response = new CliActionResponse(result, message);
            }
            else
            {
                var table = CliTableExtensions.GetTable(result.Data);
                response = new CliActionResponse(result, table);
            }

            return response;
        }

        [Action("get")]
        [Action("inspect")]
        public static async Task<CliActionResponse> GetJobDetails(CliJobOrTriggerKey jobKey)
        {
            var restRequest = new RestRequest("job/{id}", Method.Get)
                .AddParameter("id", jobKey.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<JobDetails>(restRequest);
            var tables = CliTableExtensions.GetTable(result.Data);
            return new CliActionResponse(result, tables);
        }

        [Action("next")]
        public static async Task<CliActionResponse> GetNextRunning(CliJobOrTriggerKey jobKey)
        {
            var restRequest = new RestRequest("job/nextRunning/{id}", Method.Get)
                .AddParameter("id", jobKey.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<DateTime?>(restRequest);
            var message = $"{result?.Data?.ToShortDateString()} {result?.Data?.ToShortTimeString()}";
            return new CliActionResponse(result, message: message);
        }

        [Action("prev")]
        public static async Task<CliActionResponse> GetPreviousRunning(CliJobOrTriggerKey jobKey)
        {
            var restRequest = new RestRequest("job/prevRunning/{id}", Method.Get)
                .AddParameter("id", jobKey.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<DateTime?>(restRequest);
            var message = $"{result?.Data?.ToShortDateString()} {result?.Data?.ToShortTimeString()}";
            return new CliActionResponse(result, message: message);
        }

        [Action("settings")]
        public static async Task<CliActionResponse> GetJobSettings(CliJobOrTriggerKey jobKey)
        {
            var restRequest = new RestRequest("job/{id}/settings", Method.Get)
                .AddParameter("id", jobKey.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<IEnumerable<KeyValueItem>>(restRequest);
            var table = CliTableExtensions.GetTable(result.Data);
            return new CliActionResponse(result, table);
        }

        [Action("running-ex")]
        public static async Task<CliActionResponse> GetRunningExceptions(CliFireInstanceIdRequest request)
        {
            var restRequest = new RestRequest("job/runningData/{instanceId}", Method.Get)
                .AddParameter("instanceId", request.FireInstanceId, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<GetRunningDataResponse>(restRequest);
            if (string.IsNullOrEmpty(result.Data?.Exceptions)) { return new CliActionResponse(result); }

            return new CliActionResponse(result, result.Data?.Exceptions);
        }

        [Action("running-log")]
        public static async Task<CliActionResponse> GetRunningData(CliFireInstanceIdRequest request)
        {
            var restRequest = new RestRequest("job/runningData/{instanceId}", Method.Get)
                .AddParameter("instanceId", request.FireInstanceId, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<GetRunningDataResponse>(restRequest);
            if (string.IsNullOrEmpty(result.Data?.Log)) { return new CliActionResponse(result); }

            return new CliActionResponse(result, result.Data?.Log);
        }

        [Action("running")]
        public static async Task<CliActionResponse> GetRunningJobs(CliGetRunningJobsRequest request)
        {
            var result = await GetRunningJobsInner(request);

            if (request.Quiet)
            {
                var data = result.Item1?.Select(i => i.FireInstanceId).ToList();
                var sb = new StringBuilder();
                if (data != null)
                {
                    data.ForEach(m => sb.AppendLine(m));
                }

                return new CliActionResponse(result.Item2, message: sb.ToString());
            }

            if (request.Details)
            {
                return new CliActionResponse(result.Item2, serializeObj: result.Item1);
            }

            var table = CliTableExtensions.GetTable(result.Item1);
            return new CliActionResponse(result.Item2, table);
        }

        [Action("invoke")]
        public static async Task<CliActionResponse> InvokeJob(CliInvokeJobRequest request)
        {
            var result = await InvokeJobInner(request);
            return new CliActionResponse(result);
        }

        [Action("pause-all")]
        public static async Task<CliActionResponse> PauseAll()
        {
            var restRequest = new RestRequest("job/pauseAll", Method.Post);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("pause")]
        public static async Task<CliActionResponse> PauseJob(CliJobOrTriggerKey jobKey)
        {
            var restRequest = new RestRequest("job/pause", Method.Post)
                .AddBody(jobKey);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("remove")]
        [Action("delete")]
        public static async Task<CliActionResponse> RemoveJob(CliJobOrTriggerKey jobKey)
        {
            if (!ConfirmAction($"remove job id {jobKey}")) { return CliActionResponse.Empty; }

            var restRequest = new RestRequest("job/{id}", Method.Delete)
                .AddParameter("id", jobKey.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("resume-all")]
        public static async Task<CliActionResponse> ResumeAll()
        {
            var restRequest = new RestRequest("job/resumeAll", Method.Post);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("resume")]
        public static async Task<CliActionResponse> ResumeJob(CliJobOrTriggerKey jobKey)
        {
            var restRequest = new RestRequest("job/resume", Method.Post)
                .AddBody(jobKey);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("stop")]
        public static async Task<CliActionResponse> StopRunningJob(CliFireInstanceIdRequest request)
        {
            var restRequest = new RestRequest("job/stop", Method.Post)
                .AddBody(request);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("test")]
        public static async Task<CliActionResponse> TestJob(CliInvokeJobRequest request)
        {
            var invokeDate = DateTime.Now.AddSeconds(-1);

            // (1) Invoke job
            var step1 = await TestStep1InvokeJob(request);
            if (step1 != null) { return step1; }

            // (2) Sleep 1 sec
            await Task.Delay(1000);

            // (3) Get instance id
            var step3 = await TestStep2GetInstanceId(request, invokeDate);
            if (step3.Item1 != null) { return step3.Item1; }
            var instanceId = step3.Item2;
            var logId = step3.Item3;

            // (4) Get running info
            var step4 = await TestStep4GetRunningData(instanceId, invokeDate);
            if (step4 != null) { return step4; }

            // (5) Sleep 1 sec
            await Task.Delay(1000);

            // (6) Check log
            var step6 = await TestStep6CheckLog(logId);
            if (step6 != null) { return step6; }
            return CliActionResponse.Empty;
        }

        [Action("data")]
        public static async Task<CliActionResponse> UpsertJobData(CliJobOrTriggerDataRequest request)
        {
            RestResponse result;
            switch (request.Action)
            {
                case JobDataActions.upsert:
                    var prm1 = new JobOrTriggerDataRequest
                    {
                        Id = request.Id,
                        DataKey = request.DataKey,
                        DataValue = request.DataValue
                    };

                    var restRequest1 = new RestRequest("job/data", Method.Post).AddBody(prm1);
                    result = await RestProxy.Invoke(restRequest1);

                    if (result.StatusCode == HttpStatusCode.Conflict)
                    {
                        restRequest1 = new RestRequest("job/data", Method.Put).AddBody(prm1);
                        result = await RestProxy.Invoke(restRequest1);
                    }
                    break;

                case JobDataActions.remove:
                    if (!ConfirmAction($"remove data with key '{request.DataKey}' from job {request.Id}")) { return CliActionResponse.Empty; }

                    var restRequest2 = new RestRequest("job/{id}/data/{key}", Method.Delete)
                        .AddParameter("id", request.Id, ParameterType.UrlSegment)
                        .AddParameter("key", request.DataKey, ParameterType.UrlSegment);

                    result = await RestProxy.Invoke(restRequest2);
                    break;

                default:
                    throw new CliValidationException($"action {request.Action} is not supported for this command");
            }

            AssertJobDataUpdated(result, request.Id);
            return new CliActionResponse(result);
        }

        public static async Task<string> ChooseJob()
        {
            var restRequest = new RestRequest("job", Method.Get);
            var p = AllJobsMembers.AllUserJobs;
            restRequest.AddQueryParameter("filter", (int)p);
            var result = await RestProxy.Invoke<List<JobRowDetails>>(restRequest);
            if (!result.IsSuccessful)
            {
                throw new CliException($"fail to fetch list of jobs. error message: {result.ErrorMessage}");
            }

            return ChooseJob(result.Data);
        }

        public static string ChooseJob(IEnumerable<JobRowDetails> data)
        {
            if (data.Count() <= 20)
            {
                return ShowJobsMenu(data);
            }

            var group = ShowGroupsMenu(data);
            return ShowJobsMenu(data, group);
        }

        public static string ChooseGroup(IEnumerable<JobRowDetails> data)
        {
            return ShowGroupsMenu(data);
        }

        private static string ShowJobsMenu(IEnumerable<JobRowDetails> data, string groupName = null)
        {
            var query = data.AsQueryable();

            if (!string.IsNullOrEmpty(groupName))
            {
                query = query.Where(d => d.Group.ToLower() == groupName.ToLower());
            }

            var jobs = query
                .OrderBy(d => d.Group)
                .ThenBy(d => d.Name)
                .Select(d => $"{d.Group}.{d.Name}")
                .ToList();

            return PromptSelection(jobs, "job");
        }

        private static string ShowGroupsMenu(IEnumerable<JobRowDetails> data)
        {
            var groups = data
                .OrderBy(d => d.Group)
                .Select(d => $"{d.Group}")
                .Distinct()
                .ToList();

            return PromptSelection(groups, "job group");
        }

        public static async Task<string> ChooseTrigger()
        {
            var jobId = await ChooseJob();
            var restRequest = new RestRequest("trigger/{jobId}/byjob", Method.Get);
            restRequest.AddUrlSegment("jobId", jobId);
            var result = await RestProxy.Invoke<TriggerRowDetails>(restRequest);
            if (result.IsSuccessful)
            {
                var triggers = result.Data.SimpleTriggers
                    .OrderBy(d => d.Name)
                    .Select(d => $"{d.Group}.{d.Name}")
                    .Union(
                         result.Data.CronTriggers
                         .OrderBy(d => d.Name)
                        .Select(d => $"{d.Group}.{d.Name}")
                    )
                    .ToList();

                return PromptSelection(triggers, "trigger");
            }
            else
            {
                throw new CliException($"fail to fetch list of jobs. error message: {result.ErrorMessage}");
            }
        }

        internal static async Task<(List<RunningJobDetails>, RestResponse)> GetRunningJobsInner(CliGetRunningJobsRequest request)
        {
            if (request.Iterative && request.Details)
            {
                throw new CliException("running command can't accept both 'iterative' and 'details' parameters");
            }

            RestRequest restRequest;
            RestResponse restResponse;
            List<RunningJobDetails> resultData = null;

            if (string.IsNullOrEmpty(request.FireInstanceId))
            {
                restRequest = new RestRequest("job/running", Method.Get);
                var result = await RestProxy.Invoke<List<RunningJobDetails>>(restRequest);
                resultData = result.Data;
                restResponse = result;
            }
            else
            {
                restRequest = new RestRequest("job/running/{instanceId}", Method.Get)
                    .AddParameter("instanceId", request.FireInstanceId, ParameterType.UrlSegment);
                var result = await RestProxy.Invoke<RunningJobDetails>(restRequest);
                if (result.Data != null)
                {
                    resultData = new List<RunningJobDetails> { result.Data };
                }

                restResponse = result;
            }

            return (resultData, restResponse);
        }

        private static async Task<RestResponse<LastInstanceId>> GetLastInstanceId(string id, DateTime invokeDate)
        {
            // UTC
            var dateParameter = invokeDate.ToString("s", CultureInfo.InvariantCulture);

            var restRequest = new RestRequest("job/{id}/lastInstanceId", Method.Get)
                .AddParameter("id", id, ParameterType.UrlSegment)
                .AddParameter("invokeDate", dateParameter, ParameterType.QueryString);
            var result = await RestProxy.Invoke<LastInstanceId>(restRequest);
            return result;
        }

        private static async Task<RestResponse> InvokeJobInner(CliInvokeJobRequest request)
        {
            var prm = JsonMapper.Map<InvokeJobRequest, CliInvokeJobRequest>(request);
            if (prm.NowOverrideValue == DateTime.MinValue) { prm.NowOverrideValue = null; }

            var restRequest = new RestRequest("job/invoke", Method.Post)
                .AddBody(prm);
            var result = await RestProxy.Invoke(restRequest);
            return result;
        }

        private static async Task<CliActionResponse> TestStep1InvokeJob(CliInvokeJobRequest request)
        {
            // (1) Invoke job
            AnsiConsole.MarkupLine(" [gold3_1][[x]][/] Invoke job...");
            var result = await InvokeJobInner(request);
            if (result.IsSuccessful)
            {
                return null;
            }

            return new CliActionResponse(result);
        }

        private static async Task<(CliActionResponse, string, int)> TestStep2GetInstanceId(CliInvokeJobRequest request, DateTime invokeDate)
        {
            AnsiConsole.Markup(" [gold3_1][[x]][/] Get instance id... ");
            RestResponse<LastInstanceId> instanceId = null;
            for (int i = 0; i < 20; i++)
            {
                instanceId = await GetLastInstanceId(request.Id, invokeDate);
                if (instanceId.IsSuccessful == false)
                {
                    return (new CliActionResponse(instanceId), null, 0);
                }

                if (instanceId.Data != null) break;
                await Task.Delay(1000);
            }

            if (instanceId == null || instanceId.Data == null)
            {
                AnsiConsole.WriteLine();
                throw new CliException("could not found running instance id");
            }

            AnsiConsole.MarkupLine($"[turquoise2]{instanceId.Data.InstanceId}[/]");
            return (null, instanceId.Data.InstanceId, instanceId.Data.LogId);
        }

        private static async Task<CliActionResponse> TestStep4GetRunningData(string instanceId, DateTime invokeDate)
        {
            var restRequest = new RestRequest("job/running/{instanceId}", Method.Get)
                .AddParameter("instanceId", instanceId, ParameterType.UrlSegment);
            var runResult = await RestProxy.Invoke<RunningJobDetails>(restRequest);

            if (runResult.IsSuccessful == false) { return new CliActionResponse(runResult); }
            Console.WriteLine();
            var sleepTime = 2000;
            while (runResult.Data != null)
            {
                Console.CursorTop -= 1;
                var span = DateTime.Now.Subtract(invokeDate);
                AnsiConsole.MarkupLine($" [gold3_1][[x]][/] Progress: [wheat1]{runResult.Data.Progress}[/]%  |  Effected Row(s): [wheat1]{runResult.Data.EffectedRows.GetValueOrDefault()}  |  Run Time: {CliTableFormat.FormatTimeSpan(span)}[/]     ");
                Thread.Sleep(sleepTime);
                runResult = await RestProxy.Invoke<RunningJobDetails>(restRequest);
                if (runResult.IsSuccessful == false) { break; }
                if (span.TotalMinutes >= 5) { sleepTime = 10000; }
                else if (span.TotalMinutes >= 15) { sleepTime = 20000; }
                else if (span.TotalMinutes >= 30) { sleepTime = 30000; }
            }

            Console.CursorTop -= 1;
            AnsiConsole.Markup($" [gold3_1][[x]][/] Progress: [green]100%[/]  |  ");

            return null;
        }

        private static async Task<CliActionResponse> TestStep6CheckLog(int logId)
        {
            var restTestRequest = new RestRequest("job/testStatus/{id}", Method.Get)
                .AddParameter("id", logId, ParameterType.UrlSegment);
            var status = await RestProxy.Invoke<GetTestStatusResponse>(restTestRequest);

            if (status.IsSuccessful == false) { return new CliActionResponse(status); }
            if (status.Data == null)
            {
                Console.WriteLine();
                throw new CliException($"could not found log data for log id {logId}");
            }

            var finalSpan = TimeSpan.FromMilliseconds(status.Data.Duration.GetValueOrDefault());
            AnsiConsole.Markup($"Effected Row(s): {status.Data.EffectedRows.GetValueOrDefault()}");
            AnsiConsole.MarkupLine($"  |  Run Time: {CliTableFormat.FormatTimeSpan(finalSpan)}");
            AnsiConsole.Markup(" [gold3_1][[x]][/] ");
            if (status.Data.Status == 0)
            {
                AnsiConsole.Markup("[green]Success[/]");
            }
            else
            {
                AnsiConsole.Markup($"[red]Fail (status {status.Data.Status})[/]");
            }

            Console.WriteLine();
            Console.WriteLine();

            var table = new Table();
            table.AddColumn(new TableColumn(new Markup("[grey54]Get more information by the following commands[/]")));
            table.BorderColor(Color.FromInt32(242));
            table.AddRow($"[grey54]history get[/] [grey62]{logId}[/]");
            table.AddRow($"[grey54]history log[/] [grey62]{logId}[/]");
            table.AddRow($"[grey54]history data[/] [grey62]{logId}[/]");

            if (status.Data.Status == 1)
            {
                table.AddRow($"[grey54]history ex[/] [grey62]{logId}[/]");
            }

            AnsiConsole.Write(table);

            return null;
        }
    }
}
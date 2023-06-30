using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.CliGeneral;
using Planar.CLI.Entities;
using Planar.CLI.Exceptions;
using Planar.CLI.General;
using Planar.CLI.Proxy;
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
    [Module("job", "Actions to add, remove, list, update and operate jobs", Synonyms = "jobs")]
    public class JobCliActions : BaseCliAction<JobCliActions>
    {
        [Action("add")]
        [NullRequest]
        public static async Task<CliActionResponse> AddJob(CliAddJobRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                var wrapper = await GetCliAddJobRequest(cancellationToken);
                if (!wrapper.IsSuccessful || wrapper.Request == null)
                {
                    return new CliActionResponse(wrapper.FailResponse);
                }

                request = wrapper.Request;
            }

            var body = new SetJobPathRequest { Folder = request.Folder };
            var restRequest = new RestRequest("job/folder", Method.Post)
                .AddBody(body);
            var result = await RestProxy.Invoke<JobIdResponse>(restRequest, cancellationToken);

            AssertCreated(result);
            return new CliActionResponse(result);
        }

        [IgnoreHelp]
        public static string? ChooseGroup(IEnumerable<JobRowDetails> data)
        {
            return ShowGroupsMenu(data);
        }

        [IgnoreHelp]
        public static async Task<string> ChooseJob(CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("job", Method.Get);
            var p = AllJobsMembers.AllUserJobs;
            restRequest.AddQueryParameter("filter", (int)p);
            var result = await RestProxy.Invoke<List<JobRowDetails>>(restRequest, cancellationToken);
            if (!result.IsSuccessful)
            {
                var message = "fail to fetch list of jobs";
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    message += $". error message: {result.ErrorMessage}";
                }

                throw new CliException(message, result.ErrorException);
            }

            return ChooseJob(result.Data);
        }

        [IgnoreHelp]
        public static string ChooseJob(IEnumerable<JobRowDetails>? data)
        {
            if (data == null) { return string.Empty; }

            if (data.Count() <= 20)
            {
                return ShowJobsMenu(data);
            }

            var group = ShowGroupsMenu(data);
            return ShowJobsMenu(data, group);
        }

        [IgnoreHelp]
        public static async Task<string> ChooseTrigger(CancellationToken cancellationToken = default)
        {
            var jobId = await ChooseJob(cancellationToken);
            var restRequest = new RestRequest("trigger/{jobId}/byjob", Method.Get);
            restRequest.AddUrlSegment("jobId", jobId);
            var result = await RestProxy.Invoke<TriggerRowDetails>(restRequest, cancellationToken);
            if (result.IsSuccessful && result.Data != null)
            {
                var triggers = result.Data.SimpleTriggers
                    .Select(d => new { d.Id, d.TriggerName })
                    .Union(
                         result.Data.CronTriggers
                        .Select(d => new { d.Id, d.TriggerName })
                    )
                    .ToList();

                var triggersNames = triggers.Select(t => t.TriggerName).OrderBy(t => t).ToList();
                var name = PromptSelection(triggersNames, "trigger") ?? string.Empty;
                var id = triggers.First(t => t.TriggerName == name).Id;
                return id;
            }
            else
            {
                throw new CliException($"fail to fetch list of triggers. error message: {result.ErrorMessage}");
            }
        }

        [Action("ls")]
        [Action("list")]
        public static async Task<CliActionResponse> GetAllJobs(CliGetAllJobsRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job", Method.Get);
            var p = AllJobsMembers.AllUserJobs;
            if (request.System) { p = AllJobsMembers.AllSystemJobs; }
            restRequest.AddQueryParameter("filter", (int)p);

            if (!string.IsNullOrEmpty(request.JobType))
            {
                restRequest.AddQueryParameter("jobType", request.JobType);
            }

            if (!string.IsNullOrEmpty(request.JobGroup))
            {
                restRequest.AddQueryParameter("group", request.JobGroup);
            }

            if (request.Active ^ request.Inactive)
            {
                if (request.Active)
                {
                    restRequest.AddQueryParameter("active", true.ToString());
                }

                if (request.Inactive)
                {
                    restRequest.AddQueryParameter("active", false.ToString());
                }
            }

            var result = await RestProxy.Invoke<List<JobRowDetails>>(restRequest, cancellationToken);
            var message = string.Empty;
            CliActionResponse response;
            if (request.Quiet)
            {
                var ids = result.Data?.Select(r => r.Id);
                if (ids != null)
                {
                    message = string.Join('\n', ids);
                }

                response = new CliActionResponse(result, message);
            }
            else
            {
                var table = CliTableExtensions.GetTable(result.Data);
                response = new CliActionResponse(result, table);
            }

            return response;
        }

        [Action("describe")]
        public static async Task<CliActionResponse> GetJobInfo(CliJobKey jobKey, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/{id}/info", Method.Get)
                .AddParameter("id", jobKey.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<JobDescription>(restRequest, cancellationToken);
            var tables = CliTableExtensions.GetTable(result.Data?.Details);
            var tables2 = CliTableExtensions.GetTable(result.Data?.History.ToList(), singleJob: true);
            var tables3 = CliTableExtensions.GetTable(result.Data?.Monitors.ToList());
            var tables4 = CliTableExtensions.GetTable(result.Data?.Audits?.ToList());
            var table5 = CliTableExtensions.GetTable(result.Data?.Statistics);

            tables[0].Title = "Jobs";
            tables[1].Title = "Triggers";
            tables2.Title = "History";
            tables3.Title = "Monitors";
            tables4.Title = "Audits";
            table5.Title = "Statistics";

            tables.AddRange(new CliTable[] { tables2, tables3, tables4, table5 });
            return new CliActionResponse(result, tables);
        }

        [Action("get")]
        [Action("inspect")]
        public static async Task<CliActionResponse> GetJobDetails(CliJobKey jobKey, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/{id}", Method.Get)
                .AddParameter("id", jobKey.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<JobDetails>(restRequest, cancellationToken);
            var tables = CliTableExtensions.GetTable(result.Data);
            return new CliActionResponse(result, tables);
        }

        [Action("audit")]
        public static async Task<CliActionResponse> GetJobAudits(CliJobKey jobKey, CancellationToken cancellationToken = default)
        {
            if (int.TryParse(jobKey.Id, out var id) && id > 0)
            {
                return await GetJobAudit(id, cancellationToken);
            }

            var restRequest = new RestRequest("job/{id}/audit", Method.Get)
                .AddParameter("id", jobKey.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<IEnumerable<JobAuditDto>>(restRequest, cancellationToken);
            var tables = CliTableExtensions.GetTable(result.Data);
            return new CliActionResponse(result, tables);
        }

        private static async Task<CliActionResponse> GetJobAudit(int auditId, CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("job/audit/{auditId}", Method.Get)
                .AddParameter("auditId", auditId, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<JobAuditWithInfoDto>(restRequest, cancellationToken);
            return new CliActionResponse(result, dumpObject: result.Data);
        }

        [Action("all-audits")]
        public static async Task<CliActionResponse> GetAudits(CliGetAuditsRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/audits", Method.Get)
                .AddParameter("pageNumber", request.PageNumber, ParameterType.QueryString)
                .AddParameter("pageSize", request.PageSize, ParameterType.QueryString);

            var result = await RestProxy.Invoke<IEnumerable<JobAuditDto>>(restRequest, cancellationToken);
            var tables = CliTableExtensions.GetTable(result.Data, withJobId: true);
            return new CliActionResponse(result, tables);
        }

        [Action("jobfile")]
        [NullRequest]
        public static async Task<CliActionResponse> GetJobFile(CliGetJobFileRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                var wrapper = await GetCliGetJobTypesRequest(cancellationToken);
                if (!wrapper.IsSuccessful || wrapper.Request == null)
                {
                    return new CliActionResponse(wrapper.FailResponse);
                }

                request = wrapper.Request;
            }

            var restRequest = new RestRequest("job/jobfile/{name}", Method.Get)
                .AddUrlSegment("name", request.Name);

            var result = await RestProxy.Invoke<string>(restRequest, cancellationToken);

            return new CliActionResponse(result, result.Data);
        }

        [Action("types")]
        public static async Task<CliActionResponse> GetJobTypes(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/types", Method.Get);

            var result = await RestProxy.Invoke<IEnumerable<string>>(restRequest, cancellationToken);
            return new CliActionResponse(result, result.Data);
        }

        [Action("settings")]
        public static async Task<CliActionResponse> GetJobSettings(CliJobKey jobKey, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/{id}/settings", Method.Get)
                .AddParameter("id", jobKey.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<IEnumerable<KeyValueItem>>(restRequest, cancellationToken);
            var table = CliTableExtensions.GetTable(result.Data);
            return new CliActionResponse(result, table);
        }

        [Action("next")]
        public static async Task<CliActionResponse> GetNextRunning(CliJobKey jobKey, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/{id}/nextRunning", Method.Get)
                .AddParameter("id", jobKey.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<DateTime?>(restRequest, cancellationToken);
            var message = $"{result?.Data?.ToShortDateString()} {result?.Data?.ToShortTimeString()}";
            return new CliActionResponse(result, message: message);
        }

        [Action("prev")]
        public static async Task<CliActionResponse> GetPreviousRunning(CliJobKey jobKey, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/{id}/prevRunning", Method.Get)
                .AddParameter("id", jobKey.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<DateTime?>(restRequest, cancellationToken);
            var message = $"{result?.Data?.ToShortDateString()} {result?.Data?.ToShortTimeString()}";
            return new CliActionResponse(result, message: message);
        }

        [Action("running-log")]
        public static async Task<CliActionResponse> GetRunningData(CliFireInstanceIdRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/{instanceId}/runningData", Method.Get)
                .AddParameter("instanceId", request.FireInstanceId, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<GetRunningDataResponse>(restRequest, cancellationToken);
            if (string.IsNullOrEmpty(result.Data?.Log)) { return new CliActionResponse(result); }

            return new CliActionResponse(result, result.Data?.Log);
        }

        [Action("running-ex")]
        public static async Task<CliActionResponse> GetRunningExceptions(CliFireInstanceIdRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/{instanceId}/runningData", Method.Get)
                .AddParameter("instanceId", request.FireInstanceId, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<GetRunningDataResponse>(restRequest, cancellationToken);
            if (string.IsNullOrEmpty(result.Data?.Exceptions)) { return new CliActionResponse(result); }

            return new CliActionResponse(result, result.Data?.Exceptions);
        }

        [Action("running")]
        public static async Task<CliActionResponse> GetRunningJobs(CliGetRunningJobsRequest request, CancellationToken cancellationToken = default)
        {
            var result = await GetRunningJobsInner(request, cancellationToken);

            if (request.Quiet)
            {
                var data = result.Item1?.Select(i => i.FireInstanceId).ToList();
                var sb = new StringBuilder();
                data?.ForEach(m => sb.AppendLine(m));

                return new CliActionResponse(result.Item2, message: sb.ToString());
            }

            if (request.Details)
            {
                return new CliActionResponse(result.Item2, dumpObject: result.Item1);
            }

            var table = CliTableExtensions.GetTable(result.Item1);
            return new CliActionResponse(result.Item2, table);
        }

        [Action("invoke")]
        public static async Task<CliActionResponse> InvokeJob(CliInvokeJobRequest request, CancellationToken cancellationToken = default)
        {
            var result = await InvokeJobInner(request, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("queue-invoke")]
        public static async Task<CliActionResponse> QueueInvokeJob(CliQueueInvokeJobRequest request, CancellationToken cancellationToken = default)
        {
            var prm = JsonMapper.Map<QueueInvokeJobRequest, CliQueueInvokeJobRequest>(request);
            prm ??= new QueueInvokeJobRequest();
            if (prm.Timeout == TimeSpan.Zero) { prm.Timeout = null; }

            var restRequest = new RestRequest("job/queue-invoke", Method.Post)
                .AddBody(prm);
            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("pause-all")]
        public static async Task<CliActionResponse> PauseAll(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/pauseAll", Method.Post);

            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("pause")]
        public static async Task<CliActionResponse> PauseJob(CliJobKey jobKey, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/pause", Method.Post)
                .AddBody(jobKey);

            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("remove")]
        [Action("delete")]
        public static async Task<CliActionResponse> RemoveJob(CliJobKey jobKey, CancellationToken cancellationToken = default)
        {
            if (!ConfirmAction($"remove job id {jobKey.Id}")) { return CliActionResponse.Empty; }

            var restRequest = new RestRequest("job/{id}", Method.Delete)
                .AddParameter("id", jobKey.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("resume-all")]
        public static async Task<CliActionResponse> ResumeAll(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/resumeAll", Method.Post);

            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("resume")]
        public static async Task<CliActionResponse> ResumeJob(CliJobKey jobKey, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/resume", Method.Post)
                .AddBody(jobKey);

            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("cancel")]
        [NullRequest]
        public static async Task<CliActionResponse> CancelRunningJob(CliFireInstanceIdRequest request, CancellationToken cancellationToken = default)
        {
            request ??= new CliFireInstanceIdRequest
            {
                FireInstanceId = await ChooseRunningJobInstance()
            };

            var restRequest = new RestRequest("job/cancel", Method.Post)
                .AddBody(request);

            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("test")]
        public static async Task<CliActionResponse> TestJob(CliInvokeJobRequest request, CancellationToken cancellationToken = default)
        {
            var invokeDate = DateTime.Now.AddSeconds(-1);

            // (0) Check the job
            var step0 = await TestStep0CheckJob(request, cancellationToken);
            if (step0 != null) { return step0; }

            // (1) Invoke job
            var step1 = await TestStep1InvokeJob(request, cancellationToken);
            if (step1 != null) { return step1; }

            // (2) Sleep 1 sec
            await Task.Delay(1000, cancellationToken);

            // (3) Get instance id
            var step3 = await TestStep2GetInstanceId(request, invokeDate, cancellationToken);
            if (step3.Response != null) { return step3.Response; }
            var instanceId = step3.InstanceId;
            var logId = step3.LogId;

            // (4) Get running info
            var step4 = await TestStep4GetRunningData(instanceId, invokeDate, cancellationToken);
            if (step4 != null) { return step4; }

            // (5) Sleep 1 sec
            await Task.Delay(1000, cancellationToken);

            // (6) Check log
            var step6 = await TestStep6CheckLog(logId, cancellationToken);
            if (step6 != null) { return step6; }
            return CliActionResponse.Empty;
        }

        [Action("update")]
        [ActionWizard]
        [NullRequest]
        public static async Task<CliActionResponse> UpdateJob(CliUpdateJobRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                var wrapper = await GetCliUpdateJobRequest(cancellationToken);
                if (!wrapper.IsSuccessful || wrapper.Request == null)
                {
                    return new CliActionResponse(wrapper.FailResponse);
                }

                request = wrapper.Request;
            }

            UpdateJobOptions options;

            if (request.Options == null)
            {
                options = MapUpdateJobOptions();
            }
            else
            {
                options = MapUpdateJobOptions(request.Options.Value);
            }

            var body = new UpdateJobPathRequest { Folder = request.Folder, UpdateJobOptions = options };
            var restRequest = new RestRequest("job/folder", Method.Put)
                .AddBody(body);

            var result = await RestProxy.Invoke<JobIdResponse>(restRequest, cancellationToken);
            AssertJobUpdated(result);
            return new CliActionResponse(result);
        }

        [Action("data")]
        public static async Task<CliActionResponse> PutJobData(CliJobDataRequest request, CancellationToken cancellationToken = default)
        {
            RestResponse result;
            switch (request.Action)
            {
                case DataActions.Put:
                    var prm1 = new JobOrTriggerDataRequest
                    {
                        Id = request.Id,
                        DataKey = request.DataKey,
                    };

                    if (request.DataValue != null)
                    {
                        prm1.DataValue = request.DataValue;
                    }

                    var restRequest1 = new RestRequest("job/data", Method.Post).AddBody(prm1);
                    result = await RestProxy.Invoke(restRequest1, cancellationToken);

                    if (result.StatusCode == HttpStatusCode.Conflict)
                    {
                        restRequest1 = new RestRequest("job/data", Method.Put).AddBody(prm1);
                        result = await RestProxy.Invoke(restRequest1, cancellationToken);
                    }
                    break;

                case DataActions.Remove:
                    if (!ConfirmAction($"remove data with key '{request.DataKey}' from job {request.Id}")) { return CliActionResponse.Empty; }

                    var restRequest2 = new RestRequest("job/{id}/data/{key}", Method.Delete)
                        .AddParameter("id", request.Id, ParameterType.UrlSegment)
                        .AddParameter("key", request.DataKey, ParameterType.UrlSegment);

                    result = await RestProxy.Invoke(restRequest2, cancellationToken);
                    break;

                default:
                    throw new CliValidationException($"action {request.Action} is not supported for this command");
            }

            AssertJobDataUpdated(result, request.Id);
            return new CliActionResponse(result);
        }

        private static async Task<string> ChooseRunningJobInstance()
        {
            var result = await GetRunningJobsInner(new CliGetRunningJobsRequest { });

            if (!result.Item2.IsSuccessful || result.Item1 == null)
            {
                throw new CliException($"fail to fetch list of running job instance. error message: {result.Item2.ErrorMessage}");
            }

            var items = result.Item1
                .Select(i => $"{i.FireInstanceId} ({i.Group}.{i.Name})  [{i.Progress}%]".EscapeMarkup())
                .ToList();

            if (!items.Any())
            {
                throw new CliWarningException("no running job instance(s)");
            }

            var selection = PromptSelection(items, "running job instance") ?? string.Empty;
            var parts = selection.Split(' ');
            return parts[0];
        }

        internal static async Task<(List<RunningJobDetails>?, RestResponse)> GetRunningJobsInner(CliGetRunningJobsRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Iterative && request.Details)
            {
                throw new CliException("running command can't accept both 'iterative' and 'details' parameters");
            }

            RestRequest restRequest;
            RestResponse restResponse;
            List<RunningJobDetails>? resultData = null;

            if (string.IsNullOrEmpty(request.FireInstanceId))
            {
                restRequest = new RestRequest("job/running", Method.Get);
                var result = await RestProxy.Invoke<List<RunningJobDetails>>(restRequest, cancellationToken);
                resultData = result.Data;
                restResponse = result;
            }
            else
            {
                restRequest = new RestRequest("job/running/{instanceId}", Method.Get)
                    .AddParameter("instanceId", request.FireInstanceId, ParameterType.UrlSegment);
                var result = await RestProxy.Invoke<RunningJobDetails>(restRequest, cancellationToken);
                if (result.Data != null)
                {
                    resultData = new List<RunningJobDetails> { result.Data };
                }

                restResponse = result;
            }

            return (resultData, restResponse);
        }

        private static async Task<RequestBuilderWrapper<CliAddJobRequest>> GetCliAddJobRequest(CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("job/available-jobs", Method.Get);
            var result = await RestProxy.Invoke<List<AvailableJobToAdd>>(restRequest, cancellationToken);
            if (!result.IsSuccessful)
            {
                return new RequestBuilderWrapper<CliAddJobRequest> { FailResponse = result };
            }

            var folder = SelectJobFolder(result.Data);
            var request = new CliAddJobRequest { Folder = folder };
            return new RequestBuilderWrapper<CliAddJobRequest> { Request = request };
        }

        private static async Task<RequestBuilderWrapper<CliGetJobFileRequest>> GetCliGetJobTypesRequest(CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("job/types", Method.Get);

            var result = await RestProxy.Invoke<IEnumerable<string>>(restRequest, cancellationToken);
            if (!result.IsSuccessful)
            {
                return new RequestBuilderWrapper<CliGetJobFileRequest> { FailResponse = result };
            }

            var selectedItem = PromptSelection(result.Data, "job file template") ?? string.Empty;
            var request = new CliGetJobFileRequest { Name = selectedItem };
            return new RequestBuilderWrapper<CliGetJobFileRequest> { Request = request };
        }

        private static async Task<RequestBuilderWrapper<CliUpdateJobRequest>> GetCliUpdateJobRequest(CancellationToken cancellationToken)
        {
            var jobId = await ChooseJob(cancellationToken);

            var restRequest = new RestRequest("job/{id}", Method.Get)
                 .AddParameter("id", jobId, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<JobDetails>(restRequest, cancellationToken);

            if (!result.IsSuccessful || result.Data == null)
            {
                return new RequestBuilderWrapper<CliUpdateJobRequest> { FailResponse = result };
            }

            var folder = GetPathFromProperties(result.Data.Properties);

            if (string.IsNullOrEmpty(folder))
            {
                throw new CliException($"could not find the path of the job id {jobId}");
            }

            var request = new CliUpdateJobRequest { Folder = folder };
            return new RequestBuilderWrapper<CliUpdateJobRequest> { Request = request };
        }

        private static string GetPathFromProperties(string yml)
        {
            const string path = "path:";
            if (string.IsNullOrEmpty(yml)) { return string.Empty; }

            var lines = yml.Split('\n');
            var pathLine = Array.Find(lines, p => p.ToLower().StartsWith(path));
            if (string.IsNullOrEmpty(pathLine)) { return string.Empty; }

            return pathLine[path.Length..].Trim();
        }

        private static async Task<RestResponse<LastInstanceId>> GetLastInstanceId(string id, DateTime invokeDate, CancellationToken cancellationToken)
        {
            // UTC
            var dateParameter = invokeDate.ToString("s", CultureInfo.InvariantCulture);

            var restRequest = new RestRequest("job/{id}/lastInstanceId", Method.Get)
                .AddParameter("id", id, ParameterType.UrlSegment)
                .AddParameter("invokeDate", dateParameter, ParameterType.QueryString);
            var result = await RestProxy.Invoke<LastInstanceId>(restRequest, cancellationToken);
            return result;
        }

        private static async Task<RestResponse> CheckAlreadyRunningJob(CliInvokeJobRequest request, CancellationToken cancellationToken)
        {
            var result = await CheckJobInner(cancellationToken);
            if (result.IsSuccessful)
            {
                var exists = result.Data?.Exists(d => d.Id == request.Id || $"{d.Group}.{d.Name}" == request.Id) ?? false;
                if (exists) { throw new CliException($"job id {request.Id} already running. test can not be invoked until job done"); }
            }

            return result;
        }

        private static async Task<RestResponse> InvokeJobInner(CliInvokeJobRequest request, CancellationToken cancellationToken)
        {
            var prm = JsonMapper.Map<InvokeJobRequest, CliInvokeJobRequest>(request);
            prm ??= new InvokeJobRequest();
            if (prm.NowOverrideValue == DateTime.MinValue) { prm.NowOverrideValue = null; }

            var restRequest = new RestRequest("job/invoke", Method.Post)
                .AddBody(prm);
            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return result;
        }

        private static async Task<RestResponse<List<RunningJobDetails>>> CheckJobInner(CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("job/running", Method.Get);
            var result = await RestProxy.Invoke<List<RunningJobDetails>>(restRequest, cancellationToken);
            return result;
        }

        private static UpdateJobOptions MapUpdateJobOptions()
        {
            using var _ = new TokenBlockerScope();
            var options = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("select update options:")
                    .Required() // Not required to have a favorite fruit
                    .InstructionsText(
                        "[grey](Press [blue]<space>[/] to toggle a choise, [green]<enter>[/] to accept)[/]")
                    .PageSize(15)
                    .AddChoiceGroup("all", "job details", "job data", "properties", "triggers", "triggers data")
                    .AddChoiceGroup("all job", "job details", "job data", "properties")
                    .AddChoiceGroup("all triggers", "triggers", "triggers data")
                    .AddChoiceGroup(CliPromptUtil.CancelOption)
                    );

            CliPromptUtil.CheckForCancelOption(options);
            var result = MapUpdateJobOptions(options);
            return result;
        }

        private static UpdateJobOptions MapUpdateJobOptions(JobUpdateOptions options)
        {
            var items = new List<string> { options.ToString() };
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
                        throw new CliValidationException($"option {item} is invalid. use one or more from the following options: all,all-job,all-trigger,job,job-data,properties,triggers,triggers-data");
                }
            }

            return result;
        }

        private static string SelectJobFolder(IEnumerable<AvailableJobToAdd>? data)
        {
            if (data == null || !data.Any())
            {
                throw new CliWarningException("no available jobs found on server");
            }

            var folders = data.Select(e =>
                e.Name == e.RelativeFolder ?
                e.Name ?? string.Empty :
                $"{e.Name} ({e.RelativeFolder})");

            var selectedItem = PromptSelection(folders, "job folder") ?? string.Empty;
            const string template = @"\(([^)]+)\)";
            var regex = new Regex(template, RegexOptions.None, TimeSpan.FromMilliseconds(500));
            var matches = regex.Matches(selectedItem);

            var selectedFolder = matches.LastOrDefault();
            return selectedFolder == null ? selectedItem : selectedFolder.Value[1..^1];
        }

        private static string? ShowGroupsMenu(IEnumerable<JobRowDetails> data)
        {
            var groups = data
                .OrderBy(d => d.Group)
                .Select(d => $"{d.Group}")
                .Distinct()
                .ToList();

            return PromptSelection(groups, "job group");
        }

        private static string ShowJobsMenu(IEnumerable<JobRowDetails> data, string? groupName = null)
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

            return PromptSelection(jobs, "job") ?? string.Empty;
        }

        private static async Task<CliActionResponse?> TestStep0CheckJob(CliInvokeJobRequest request, CancellationToken cancellationToken)
        {
            // (0) Check for running job
            AnsiConsole.MarkupLine(" [gold3_1][[x]][/] Check job...");
            var result = await CheckAlreadyRunningJob(request, cancellationToken);
            if (result.IsSuccessful) { return null; }

            return new CliActionResponse(result);
        }

        private static async Task<CliActionResponse?> TestStep1InvokeJob(CliInvokeJobRequest request, CancellationToken cancellationToken)
        {
            // (1) Invoke job
            AnsiConsole.MarkupLine(" [gold3_1][[x]][/] Invoke job...");
            var result = await InvokeJobInner(request, cancellationToken);
            if (result.IsSuccessful)
            {
                return null;
            }

            return new CliActionResponse(result);
        }

        private static async Task<TestData> TestStep2GetInstanceId(CliInvokeJobRequest request, DateTime invokeDate, CancellationToken cancellationToken)
        {
            AnsiConsole.Markup(" [gold3_1][[x]][/] Get instance id... ");
            RestResponse<LastInstanceId>? instanceId = null;
            var response = new TestData();

            for (int i = 0; i < 20; i++)
            {
                instanceId = await GetLastInstanceId(request.Id, invokeDate, cancellationToken);
                if (!instanceId.IsSuccessful)
                {
                    AnsiConsole.WriteLine();
                    response.Response = new CliActionResponse(instanceId);
                    return response;
                }

                if (instanceId.Data != null) { break; }
                if (i > 5)
                {
                    await CheckAlreadyRunningJob(request, cancellationToken);
                }

                await Task.Delay(1000, cancellationToken);
            }

            if (instanceId == null || instanceId.Data == null)
            {
                AnsiConsole.WriteLine();
                throw new CliException("could not found running instance id. check whether job is paused or maybe another instance already running");
            }

            AnsiConsole.MarkupLine($"[turquoise2]{instanceId.Data.InstanceId}[/]");
            response.InstanceId = instanceId.Data.InstanceId;
            response.LogId = instanceId.Data.LogId;
            return response;
        }

        private static async Task<CliActionResponse?> TestStep4GetRunningData(string instanceId, DateTime invokeDate, CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("job/{instanceId}/running", Method.Get)
                .AddParameter("instanceId", instanceId, ParameterType.UrlSegment);
            var runResult = await RestProxy.Invoke<RunningJobDetails>(restRequest, cancellationToken);

            if (!runResult.IsSuccessful)
            {
                // Not Found: job finish in very short time
                AnsiConsole.Markup($" [gold3_1][[x]][/] Progress: 100%  |  ");
                if (runResult.StatusCode == HttpStatusCode.NotFound) { return null; }

                // Fail to get running data
                return new CliActionResponse(runResult);
            }

            Console.WriteLine();
            var sleepTime = 2000;
            var max = 0;
            while (runResult.Data != null)
            {
                Console.CursorTop -= 1;
                var span = DateTime.Now.Subtract(invokeDate);
                var title =
                        $" [gold3_1][[x]][/] Progress: [wheat1]{runResult.Data.Progress}[/]%  |" +
                        $"  Effected Row(s): [wheat1]{runResult.Data.EffectedRows.GetValueOrDefault()}[/]  |" +
                        $"  Ex. Count: {CliTableFormat.FormatExceptionCount(runResult.Data.ExceptionsCount)}  |" +
                        $"  Run Time: [wheat1]{CliTableFormat.FormatTimeSpan(span)}[/]  |" +
                        $"  End Time: [wheat1]{CliTableFormat.FormatTimeSpan(runResult.Data.EstimatedEndTime)}[/]     ";
                max = Math.Max(max, title.Length);
                AnsiConsole.MarkupLine(title);
                await Task.Delay(sleepTime, cancellationToken);
                runResult = await RestProxy.Invoke<RunningJobDetails>(restRequest, cancellationToken);
                if (!runResult.IsSuccessful) { break; }
                if (span.TotalMinutes >= 5) { sleepTime = 10000; }
                else if (span.TotalMinutes >= 15) { sleepTime = 20000; }
                else if (span.TotalMinutes >= 30) { sleepTime = 30000; }
            }

            Console.CursorTop -= 1;

            if (cancellationToken.IsCancellationRequested)
            {
                AnsiConsole.WriteLine();
            }
            else
            {
                AnsiConsole.Markup($" [gold3_1][[x]][/] Progress: 100%  |  ");
            }

            return null;
        }

        private static async Task<CliActionResponse?> TestStep6CheckLog(long logId, CancellationToken cancellationToken)
        {
            var restTestRequest = new RestRequest("job/{id}/testStatus", Method.Get)
                .AddParameter("id", logId, ParameterType.UrlSegment);
            var status = await RestProxy.Invoke<GetTestStatusResponse>(restTestRequest, cancellationToken);

            if (!status.IsSuccessful) { return new CliActionResponse(status); }
            if (status.Data == null)
            {
                Console.WriteLine();
                throw new CliException($"could not found log data for log id {logId}");
            }

            var finalSpan = TimeSpan.FromMilliseconds(status.Data.Duration.GetValueOrDefault());
            AnsiConsole.Markup($"Effected Row(s): {status.Data.EffectedRows.GetValueOrDefault()}  |");
            AnsiConsole.Markup($"  Ex. Count: {CliTableFormat.FormatExceptionCount(status.Data.ExceptionCount)}  |");
            AnsiConsole.Markup($"  Run Time: {CliTableFormat.FormatTimeSpan(finalSpan)}  |");
            AnsiConsole.MarkupLine($"  End Time: --:--:--     ");
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

        private struct TestData
        {
            public string InstanceId { get; set; }
            public long LogId { get; set; }
            public CliActionResponse? Response { get; set; }
        }
    }
}
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
            var result = await RestProxy.Invoke<PlanarIdResponse>(restRequest, cancellationToken);

            AssertCreated(result);
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

        [IgnoreHelp]
        public static string? ChooseGroup(IEnumerable<JobBasicDetails> data)
        {
            return ShowGroupsMenu(data);
        }

        [IgnoreHelp]
        public static async Task<string> ChooseJob(string? filter, CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("job", Method.Get);
            var p = AllJobsMembers.AllUserJobs;
            restRequest.AddQueryParameter("jobCategory", (int)p)
                .AddQueryPagingParameter(1000);

            var result = await RestProxy.Invoke<PagingResponse<JobBasicDetails>>(restRequest, cancellationToken);
            if (!result.IsSuccessful)
            {
                var message = "fail to fetch list of jobs";
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    message += $". error message: {result.ErrorMessage}";
                }

                throw new CliException(message, result);
            }

            var filterData = FilterJobs(result.Data?.Data, filter);
            return ChooseJob(filterData);
        }

        [IgnoreHelp]
        public static string ChooseJob(IEnumerable<JobBasicDetails>? data)
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
        public static async Task<string> ChooseTrigger(string? filter, CancellationToken cancellationToken = default)
        {
            var jobId = await ChooseJob(filter, cancellationToken);
            var restRequest = new RestRequest("trigger/{jobId}/by-job", Method.Get);
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
                throw new CliException($"fail to fetch list of triggers. error message: {result.ErrorMessage}", result);
            }
        }

        [Action("ls")]
        [Action("list")]
        public static async Task<CliActionResponse> GetAllJobs(CliGetAllJobsRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job", Method.Get);
            var p = AllJobsMembers.AllUserJobs;
            if (request.System) { p = AllJobsMembers.AllSystemJobs; }
            restRequest.AddQueryParameter("jobCategory", (int)p);

            if (!string.IsNullOrEmpty(request.JobType))
            {
                restRequest.AddQueryParameter("jobType", request.JobType);
            }

            if (!string.IsNullOrEmpty(request.JobGroup))
            {
                restRequest.AddQueryParameter("group", request.JobGroup);
            }

            if (request.Active ^ request.Inactive) // XOR Operator
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

            if (!string.IsNullOrWhiteSpace(request.Filter))
            {
                restRequest.AddQueryParameter("filter", request.Filter);
            }

            restRequest.AddQueryPagingParameter(request);

            var result = await RestProxy.Invoke<PagingResponse<JobBasicDetails>>(restRequest, cancellationToken);
            var message = string.Empty;
            CliActionResponse response;
            if (request.Quiet)
            {
                var ids = result.Data?.Data?.Select(r => r.Id);
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

        [Action("all-audits")]
        public static async Task<CliActionResponse> GetAudits(CliPagingRequest request, CancellationToken cancellationToken = default)
        {
            request.SetPagingDefaults();
            var restRequest = new RestRequest("job/audits", Method.Get)
                .AddQueryPagingParameter(request);

            var result = await RestProxy.Invoke<PagingResponse<JobAuditDto>>(restRequest, cancellationToken);
            var tables = CliTableExtensions.GetTable(result.Data, withJobId: true);
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

            var result = await RestProxy.Invoke<PagingResponse<JobAuditDto>>(restRequest, cancellationToken);
            var tables = CliTableExtensions.GetTable(result.Data);
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

        [Action("describe")]
        public static async Task<CliActionResponse> GetJobInfo(CliJobKey jobKey, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/{id}/info", Method.Get)
                .AddParameter("id", jobKey.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<JobDescription>(restRequest, cancellationToken);
            var tables = CliTableExtensions.GetTable(result.Data?.Details);
            var tables2 = CliTableExtensions.GetTable(result.Data?.History, singleJob: true);
            var tables3 = CliTableExtensions.GetTable(result.Data?.Monitors);
            var tables4 = CliTableExtensions.GetTable(result.Data?.Audits);
            var table5 = CliTableExtensions.GetTable(result.Data?.Metrics);

            tables[0].Title = "Jobs";
            tables[1].Title = "Triggers";
            tables2.Title = "History";
            tables3.Title = "Monitors";
            tables4.Title = "Audits";
            table5.Title = "Metrics";

            tables.AddRange(new CliTable[] { tables2, tables3, tables4, table5 });
            return new CliActionResponse(result, tables);
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

        [Action("types")]
        public static async Task<CliActionResponse> GetJobTypes(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/types", Method.Get);

            var result = await RestProxy.Invoke<IEnumerable<string>>(restRequest, cancellationToken);
            return new CliActionResponse(result, result.Data);
        }

        [Action("next")]
        public static async Task<CliActionResponse> GetNextRunning(CliJobKey jobKey, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/{id}/next-running", Method.Get)
                .AddParameter("id", jobKey.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<DateTime?>(restRequest, cancellationToken);
            var message = $"{result?.Data?.ToShortDateString()} {result?.Data?.ToShortTimeString()}";
            return new CliActionResponse(result, message: message);
        }

        [Action("prev")]
        public static async Task<CliActionResponse> GetPreviousRunning(CliJobKey jobKey, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/{id}/prev-running", Method.Get)
                .AddParameter("id", jobKey.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<DateTime?>(restRequest, cancellationToken);
            var message = $"{result?.Data?.ToShortDateString()} {result?.Data?.ToShortTimeString()}";
            return new CliActionResponse(result, message: message);
        }

        [Action("running-log")]
        public static async Task<CliActionResponse> GetRunningData(CliFireInstanceIdRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/running-data/{instanceId}", Method.Get)
                .AddParameter("instanceId", request.FireInstanceId, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<RunningJobData>(restRequest, cancellationToken);
            if (string.IsNullOrEmpty(result.Data?.Log)) { return new CliActionResponse(result); }

            return new CliActionResponse(result, result.Data?.Log);
        }

        [Action("running-ex")]
        public static async Task<CliActionResponse> GetRunningExceptions(CliFireInstanceIdRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/running-data/{instanceId}", Method.Get)
                .AddParameter("instanceId", request.FireInstanceId, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<RunningJobData>(restRequest, cancellationToken);
            if (string.IsNullOrEmpty(result.Data?.Exceptions)) { return new CliActionResponse(result); }

            return new CliActionResponse(result, result.Data?.Exceptions);
        }

        [Action("running")]
        public static async Task<CliActionResponse> GetRunningJobs(CliGetRunningJobsRequest request, CancellationToken cancellationToken = default)
        {
            var result = await GetRunningJobsInner(request, cancellationToken);

            if (request.Quiet)
            {
                var data = result.Item1.Select(i => i.FireInstanceId).ToList();
                var sb = new StringBuilder();
                data.ForEach(m => sb.AppendLine(m));

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

        [Action("pause")]
        public static async Task<CliActionResponse> PauseJob(CliJobKey jobKey, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/pause", Method.Post)
                .AddBody(jobKey);

            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("data")]
        public static async Task<CliActionResponse> PutJobData(CliJobDataRequest request, CancellationToken cancellationToken = default)
        {
            FillMissingDataProperties(request);
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

        [Action("queue-invoke")]
        public static async Task<CliActionResponse> QueueInvokeJob(CliQueueInvokeJobRequest request, CancellationToken cancellationToken = default)
        {
            var prm = JsonMapper.Map<QueueInvokeJobRequest, CliQueueInvokeJobRequest>(request);
            prm ??= new QueueInvokeJobRequest();

            var restRequest = new RestRequest("job/queue-invoke", Method.Post)
                .AddBody(prm);
            var result = await RestProxy.Invoke<PlanarIdResponse>(restRequest, cancellationToken);
            AssertCreated(result);
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

        [Action("resume")]
        public static async Task<CliActionResponse> ResumeJob(CliJobKey jobKey, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("job/resume", Method.Post)
                .AddBody(jobKey);

            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("set-author")]
        [NullRequest]
        public static async Task<CliActionResponse> SetAuthor(CliSetAuthorOfJob request, CancellationToken cancellationToken = default)
        {
            request ??= new CliSetAuthorOfJob
            {
                Id = await ChooseJob(null, cancellationToken),
                Author = CollectCliValue(
                      field: "author of the job",
                      required: true,
                      minLength: 0,
                      maxLength: 200)
            };

            var restRequest = new RestRequest("job/author", Method.Patch)
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

            // (2) Get instance id
            var step3 = await TestStep2GetInstanceId(request, invokeDate, cancellationToken);
            if (step3.Response != null) { return step3.Response; }
            var instanceId = step3.InstanceId;
            var logId = step3.LogId;

            // (3) Get running info
            var step4 = await TestStep3GetRunningData(instanceId, invokeDate, cancellationToken);
            if (step4 != null) { return step4; }

            // (4) Sleep 1 sec
            await Task.Delay(500, cancellationToken);

            // (5) Check log
            var step6 = await TestStep5CheckLog(logId, cancellationToken);
            if (step6 != null) { return step6; }
            return CliActionResponse.Empty;
        }

        [Action("update")]
        [ActionWizard]
        [NullRequest]
        public static async Task<CliActionResponse> UpdateJob(CliUpdateJobRequest request, CancellationToken cancellationToken = default)
        {
            var body = new UpdateJobRequest();
            request ??= new CliUpdateJobRequest();
            if (string.IsNullOrWhiteSpace(request.Id))
            {
                body.Id = await ChooseJob(null, cancellationToken);
            }
            else
            {
                body.Id = request.Id;
            }

            if (request.Options == null)
            {
                body.Options = MapUpdateJobOptions();
            }
            else
            {
                body.Options = MapUpdateJobOptions(request.Options.Value);
            }

            var restRequest = new RestRequest("job", Method.Put)
                .AddBody(body);

            var result = await RestProxy.Invoke<PlanarIdResponse>(restRequest, cancellationToken);
            AssertJobUpdated(result);
            return new CliActionResponse(result);
        }

        internal static async Task<(List<RunningJobDetails>, RestResponse)> GetRunningJobsInner(CliGetRunningJobsRequest request, CancellationToken cancellationToken = default)
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
                restRequest = new RestRequest("job/running-instance/{instanceId}", Method.Get)
                    .AddParameter("instanceId", request.FireInstanceId, ParameterType.UrlSegment);
                var result = await RestProxy.Invoke<RunningJobDetails>(restRequest, cancellationToken);
                if (result.Data != null)
                {
                    resultData = new List<RunningJobDetails> { result.Data };
                }

                restResponse = result;
            }

            if (!restResponse.IsSuccessful || resultData == null)
            {
                throw new CliException($"fail to fetch list of running job instance. error message: {restResponse.ErrorMessage}", restResponse);
            }

            return (resultData, restResponse);
        }

        private static async Task<RestResponse> CheckAlreadyRunningJob(CliInvokeJobRequest request, CancellationToken cancellationToken)
        {
            var result = await CheckJobInner(cancellationToken);
            if (result.IsSuccessful)
            {
                var exists = result.Data?.Exists(d => d.Id == request.Id || string.Equals($"{d.Group}.{d.Name}", request.Id, StringComparison.OrdinalIgnoreCase)) ?? false;
                if (exists) { throw new CliException($"job id {request.Id} already running. test can not be invoked until job done"); }
            }

            return result;
        }

        private static async Task<RestResponse<List<RunningJobDetails>>> CheckJobInner(CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("job/running", Method.Get);
            var result = await RestProxy.Invoke<List<RunningJobDetails>>(restRequest, cancellationToken);
            return result;
        }

        private static async Task<string> ChooseRunningJobInstance()
        {
            var result = await GetRunningJobsInner(new CliGetRunningJobsRequest());

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

        private static List<JobBasicDetails>? FilterJobs(List<JobBasicDetails>? data, string? filter)
        {
            if (data == null) { return null; }
            if (string.IsNullOrWhiteSpace(filter)) { return data; }

            if (filter.StartsWith('?')) { filter = filter[1..]; }

            data = data.Where(d =>
                d.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                d.Group.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(d.Description) && d.Description.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return data;
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

        private static DateTime? GetEstimatedEndTime(RestResponse<RunningJobDetails> runResult)
        {
            if (runResult.Data == null) { return null; }
            if (runResult.Data.EstimatedEndTime == null) { return null; }
            var estimateEnd = DateTime.Now.Add(runResult.Data.EstimatedEndTime.Value);
            return estimateEnd;
        }

        private static async Task<CliActionResponse> GetJobAudit(int auditId, CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("job/audit/{auditId}", Method.Get)
                .AddParameter("auditId", auditId, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<JobAuditWithInfoDto>(restRequest, cancellationToken);
            return new CliActionResponse(result, dumpObject: result.Data);
        }

        private static async Task<RestResponse<LastInstanceId>> GetLastInstanceId(string id, DateTime invokeDate, CancellationToken cancellationToken)
        {
            // UTC
            var dateParameter = invokeDate.ToString("s", CultureInfo.InvariantCulture);

            var restRequest = new RestRequest("job/{id}/last-instance-id/long-polling", Method.Get)
                .AddParameter("id", id, ParameterType.UrlSegment)
                .AddParameter("invokeDate", dateParameter, ParameterType.QueryString);

            restRequest.Timeout = 35_000; // 35 sec

            var result = await RestProxy.Invoke<LastInstanceId>(restRequest, cancellationToken);
            return result;
        }

        private static async Task<(CliActionResponse?, bool, RestResponse<RunningJobDetails>)> InitGetRunningData(string instanceId, CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("job/running-instance/{instanceId}", Method.Get)
                .AddParameter("instanceId", instanceId, ParameterType.UrlSegment);
            var runResult = await RestProxy.Invoke<RunningJobDetails>(restRequest, cancellationToken);

            if (!runResult.IsSuccessful)
            {
                // Not Found: job finish in very short time
                AnsiConsole.Markup($" [gold3_1][[x]][/] Progress: 100%  |  ");
                if (runResult.StatusCode == HttpStatusCode.NotFound) { return (null, true, runResult); }

                // Fail to get running data
                return (new CliActionResponse(runResult), true, runResult);
            }

            return (null, false, runResult);
        }

        private static async Task<RestResponse> InvokeJobInner(CliInvokeJobRequest request, CancellationToken cancellationToken)
        {
            var prm = JsonMapper.Map<InvokeJobRequest, CliInvokeJobRequest>(request);
            prm ??= new InvokeJobRequest();

            var restRequest = new RestRequest("job/invoke", Method.Post)
                .AddBody(prm);
            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return result;
        }

        private static async Task<(RestResponse<RunningJobDetails>, DateTime?)> LongPollingGetRunningData(
            RestResponse<RunningJobDetails> runResult,
            string instanceId,
            DateTime invokeDate,
            CancellationToken cancellationToken)
        {
            var data = runResult.Data;
            var currentHash = $"{data?.Progress ?? 0}.{data?.EffectedRows ?? 0}.{data?.ExceptionsCount ?? 0}";
            var restRequest = new RestRequest("job/running-instance/{instanceId}/long-polling", Method.Get)
                .AddParameter("instanceId", instanceId, ParameterType.UrlSegment)
                .AddQueryParameter("hash", currentHash);
            restRequest.Timeout = 300_000; // 6 min
            var counter = 1;
            while (counter <= 3)
            {
                runResult = await RestProxy.Invoke<RunningJobDetails>(restRequest, cancellationToken);
                if (runResult.IsSuccessful) { break; }
                if (runResult.StatusCode == HttpStatusCode.NotFound) { break; }
                await Task.Delay(500 + ((counter - 1) ^ 2) * 500, cancellationToken);
                counter++;
            }

            var estimateEnd = GetEstimatedEndTime(runResult);
            WriteRunningData(runResult, invokeDate, estimateEnd);

            return (runResult, estimateEnd);
        }

        private static UpdateJobOptions MapUpdateJobOptions()
        {
            using var _ = new TokenBlockerScope();
            var options = new[] { "without data (default)", "with job data", "with triggers data", "all data" };
            var selected = CliPromptUtil.PromptSelection(options, "select update options");
            CliPromptUtil.CheckForCancelOption(selected);
            var result = MapUpdateJobOptions(selected);
            return result;
        }

        private static UpdateJobOptions MapUpdateJobOptions(JobUpdateOptions options)
        {
            var result = UpdateJobOptions.Default;
            switch (options)
            {
                case JobUpdateOptions.NoData:
                    return result;

                case JobUpdateOptions.JobData:
                    result.UpdateJobData = true;
                    result.UpdateTriggersData = false;
                    break;

                case JobUpdateOptions.TriggersData:
                    result.UpdateJobData = false;
                    result.UpdateTriggersData = true;
                    break;

                case JobUpdateOptions.All:
                    result.UpdateJobData = true;
                    result.UpdateTriggersData = true;
                    return result;

                default:
                    throw new CliValidationException($"option {options} is invalid. use one or more from the following options: no-data, job-data, triggers-data, all");
            }

            return result;
        }

        private static UpdateJobOptions MapUpdateJobOptions(string? selected)
        {
            var result = UpdateJobOptions.Default;
            if (string.IsNullOrWhiteSpace(selected)) { return result; }

            switch (selected.ToLower())
            {
                case "without data (default)":
                    return result;

                case "with job data":
                    result.UpdateJobData = true;
                    result.UpdateTriggersData = false;
                    break;

                case "with triggers data":
                    result.UpdateJobData = false;
                    result.UpdateTriggersData = true;
                    break;

                case "all data":
                    result.UpdateJobData = true;
                    result.UpdateTriggersData = true;
                    return result;

                default:
                    throw new CliValidationException($"option {selected} is invalid. use one or more from the following options: no-data, job-data, triggers-data, all");
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

        private static string? ShowGroupsMenu(IEnumerable<JobBasicDetails> data)
        {
            var groups = data
                .OrderBy(d => d.Group)
                .Select(d => $"{d.Group}")
                .Distinct()
                .ToList();

            return PromptSelection(groups, "job group");
        }

        private static string ShowJobsMenu(IEnumerable<JobBasicDetails> data, string? groupName = null)
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

            var result = new TestData();
            var response = await GetLastInstanceId(request.Id, invokeDate, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                throw new CliException($"job id {request.Id} already running. test can not be invoked until job done");
            }

            if (!response.IsSuccessful)
            {
                AnsiConsole.WriteLine();
                result.Response = new CliActionResponse(response);
                return result;
            }

            if (response.Data == null)
            {
                AnsiConsole.WriteLine();
                throw new CliException("could not found running instance id. check whether job is paused or maybe another instance already running");
            }

            AnsiConsole.MarkupLine($"[turquoise2]{response.Data.InstanceId}[/]");
            result.InstanceId = response.Data.InstanceId;
            result.LogId = response.Data.LogId;
            return result;
        }

        private static async Task<CliActionResponse?> TestStep3GetRunningData(string instanceId, DateTime invokeDate, CancellationToken cancellationToken)
        {
            // check for very fast finish job
            var result = await InitGetRunningData(instanceId, cancellationToken);
            if (result.Item2) { return result.Item1; }

            Console.WriteLine();

            Task dataTask = Task.CompletedTask;
            var runResult = result.Item3;
            DateTime? estimateEnd = null;
            while (!cancellationToken.IsCancellationRequested)
            {
                var brk = WriteRunningData(runResult, invokeDate, estimateEnd);
                if (brk) { break; }

                if (dataTask.Status == TaskStatus.RanToCompletion)
                {
                    dataTask = Task.Run(async () =>
                    {
                        var data = await LongPollingGetRunningData(runResult, instanceId, invokeDate, cancellationToken);
                        runResult = data.Item1;
                        estimateEnd = data.Item2;
                    }, cancellationToken);
                }

                for (int i = 0; i < 5; i++)
                {
                    await Task.Delay(200, cancellationToken);
                    if (dataTask.Status == TaskStatus.RanToCompletion) { break; }
                }

                if (!runResult.IsSuccessful) { break; }
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

        private static async Task<CliActionResponse?> TestStep5CheckLog(long logId, CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("history/{id}", Method.Get)
               .AddParameter("id", logId, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<JobHistory>(restRequest, cancellationToken);

            if (result.StatusCode == HttpStatusCode.NotFound || result.Data == null)
            {
                Console.WriteLine();
                throw new CliException($"could not found log data for log id {logId}");
            }

            if (!result.IsSuccessful) { return new CliActionResponse(result); }

            var finalSpan = TimeSpan.FromMilliseconds(result.Data.Duration.GetValueOrDefault());
            AnsiConsole.Markup($"Effected Row(s): {result.Data.EffectedRows.GetValueOrDefault()}  |");
            AnsiConsole.Markup($"  Ex. Count: {CliTableFormat.FormatExceptionCount(result.Data.ExceptionCount)}  |");
            AnsiConsole.Markup($"  Run Time: {CliTableFormat.FormatTimeSpan(finalSpan)}  |");
            AnsiConsole.MarkupLine($"  End Time: --:--:--     ");
            AnsiConsole.Markup(" [gold3_1][[x]][/] ");
            if (result.Data.Status == 0)
            {
                AnsiConsole.Markup("[green]Success[/]");
            }
            else
            {
                AnsiConsole.Markup($"[red]Fail (status {result.Data.Status})[/]");
            }

            Console.WriteLine();
            Console.WriteLine();

            var table = new Table();
            table.AddColumn(new TableColumn(new Markup("[grey54]Get more information by the following commands[/]")));
            table.BorderColor(Color.FromInt32(242));
            table.AddRow($"[grey54]history get[/] [grey62]{logId}[/]");
            table.AddRow($"[grey54]history log[/] [grey62]{logId}[/]");
            table.AddRow($"[grey54]history data[/] [grey62]{logId}[/]");

            if (result.Data.Status == 1)
            {
                table.AddRow($"[grey54]history ex[/] [grey62]{logId}[/]");
            }

            AnsiConsole.Write(table);

            return null;
        }

        private static bool WriteRunningData(RestResponse<RunningJobDetails> runResult, DateTime invokeDate, DateTime? estimateEnd)
        {
            if (runResult.Data == null) { return true; }
            var data = runResult.Data;

            Console.CursorTop -= 1;
            var span = DateTimeOffset.Now.Subtract(invokeDate);
            var endSpan = estimateEnd == null ? data.EstimatedEndTime : estimateEnd.Value.Subtract(DateTime.Now);
            var title =
                    $" [gold3_1][[x]][/] Progress: [wheat1]{data.Progress}[/]%  |" +
                    $"  Effected Row(s): [wheat1]{data.EffectedRows.GetValueOrDefault()}[/]  |" +
                    $"  Ex. Count: {CliTableFormat.FormatExceptionCount(data.ExceptionsCount)}  |" +
                    $"  Run Time: [wheat1]{CliTableFormat.FormatTimeSpan(span)}[/]  |" +
                    $"  End Time: [wheat1]{CliTableFormat.FormatTimeSpan(endSpan)}[/]     ";
            AnsiConsole.MarkupLine(title);

            return false;
        }

        private struct TestData
        {
            public string InstanceId { get; set; }
            public long LogId { get; set; }
            public CliActionResponse? Response { get; set; }
        }
    }
}
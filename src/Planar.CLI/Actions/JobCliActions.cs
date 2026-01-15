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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Actions;

[Module("job", "add, remove, list, update and operate jobs", Synonyms = "jobs")]
public class JobCliActions : BaseCliAction<JobCliActions>
{
    private static readonly Lock _locker = new();

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

        var body = new SetJobPathRequest { JobFilePath = request.Filename };
        var restRequest = new RestRequest("job", Method.Post)
            .AddBody(body);
        var result = await RestProxy.Invoke<PlanarIdResponse>(restRequest, cancellationToken);

        AssertCreated(result);
        return new CliActionResponse(result);
    }

    [Action("cancel")]
    [NullRequest]
    public static async Task<CliActionResponse> CancelRunningJob(CliFireInstanceIdRequest request, CancellationToken cancellationToken = default)
    {
        request ??= new CliFireInstanceIdRequest();
        if (string.IsNullOrWhiteSpace(request.FireInstanceId))
        {
            request.FireInstanceId = await ChooseRunningJobInstance();
        }

        var restRequest = new RestRequest("job/cancel", Method.Post)
            .AddBody(request);

        var result = await RestProxy.Invoke(restRequest, cancellationToken);
        return new CliActionResponse(result);
    }

    [IgnoreHelp]
    public static string? ChooseGroup(IEnumerable<JobBasicDetails> data, bool writeSelection)
    {
        var groups = data.Select(d => d.Group);
        return ShowGroupsMenu(groups, writeSelection);
    }

    [IgnoreHelp]
    public static async Task<string?> ChooseGroup(CancellationToken cancellationToken)
    {
        var restRequest = new RestRequest("job/groups", Method.Get);
        var result = await RestProxy.Invoke<IEnumerable<string>>(restRequest, cancellationToken);
        if (!result.IsSuccessful)
        {
            var message = "fail to fetch list of job groups";
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                message += $". error message: {result.ErrorMessage}";
            }
            throw new CliException(message, result);
        }

        if (result.Data == null || !result.Data.Any())
        {
            throw new CliWarningException("there are no job groups");
        }

        return ShowGroupsMenu(result.Data);
    }

    [IgnoreHelp]
    public static async Task<string> ChooseJob(string? filter, bool groupMenu, bool writeSelection = true, CancellationToken cancellationToken = default)
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
        return ChooseJob(filterData, groupMenu, writeSelection);
    }

    [IgnoreHelp]
    public static string ChooseJob(List<JobBasicDetails>? data, bool groupMenu, bool writeSelection = true)
    {
        if (data == null) { return string.Empty; }

        if (data.Count <= 1000 && !groupMenu)
        {
            return ShowJobsMenu(data, writeSelection: writeSelection);
        }

        var group = ShowGroupsMenu(data.Select(d => d.Group), writeSelection);
        return ShowJobsMenu(data, group, writeSelection);
    }

    [IgnoreHelp]
    public static async Task<string> ChooseTrigger(string? filter, CancellationToken cancellationToken = default)
    {
        var jobId = await ChooseJob(filter, false, writeSelection: false, cancellationToken);
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

    [Action("cb")]
    [Action("circuit-breaker")]
    public static async Task<CliActionResponse> GetCircuitBreaker(CliJobKey request, CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest("job/{id}", Method.Get)
            .AddParameter("id", request.Id, ParameterType.UrlSegment);

        var result = await RestProxy.Invoke<JobDetails>(restRequest, cancellationToken);
        if (result.Data?.CircuitBreaker == null)
        {
            throw new CliWarningException($"circuit breaker is disabled for job {request.Id}");
        }

        var table = CliTableExtensions.GetTable(result.Data?.CircuitBreaker);
        return new CliActionResponse(result, table);
    }

    [Action("audit")]
    public static async Task<CliActionResponse> GetJobAudits(CliAuditRequest request, CancellationToken cancellationToken = default)
    {
        if (int.TryParse(request.Id, out var id) && id > 0)
        {
            return await GetJobAudit(id, cancellationToken);
        }

        var restRequest = new RestRequest("job/{id}/audit", Method.Get)
            .AddParameter("id", request.Id, ParameterType.UrlSegment)
            .AddQueryPagingParameter(request);

        var result = await RestProxy.Invoke<PagingResponse<JobAuditDto>>(restRequest, cancellationToken);
        var tables = CliTableExtensions.GetTable(result.Data);
        return new CliActionResponse(result, tables);
    }

    [Action("get")]
    public static async Task<CliActionResponse> GetJobDetails(CliJobKey request, CancellationToken cancellationToken = default)
    {
        var result = await GetJob(request.Id, cancellationToken);
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

        tables.AddRange([tables2, tables3, tables4, table5]);
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
    [NullRequest]
    public static async Task<CliActionResponse> GetRunningData(CliRunningLogRequest request, CancellationToken cancellationToken)
    {
        request ??= new CliRunningLogRequest();
        if (string.IsNullOrWhiteSpace(request.FireInstanceId))
        {
            request.FireInstanceId = await ChooseRunningJobInstance();
        }

        if (request.Live)
        {
            var builder = new UriBuilder(RestProxy.BaseUri)
            {
                Path = $"job/running-log/{request.FireInstanceId}/sse",
            };

            using var client = new HttpClient();
            var uri = builder.Uri;

            client.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var restResponse = await CliActionResponse.Convert(response);
                return new CliActionResponse(restResponse);
            }

            Console.Clear();
            using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(body);
            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var value = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(value)) { continue; }
                var log = CliFormat.GetLogMarkup(value) ?? string.Empty;
                AnsiConsole.Markup(log);
            }

            return CliActionResponse.Empty;
        }
        else
        {
            var restRequest = new RestRequest("job/running-data/{instanceId}", Method.Get)
                .AddParameter("instanceId", request.FireInstanceId, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<RunningJobData>(restRequest, cancellationToken);
            if (string.IsNullOrEmpty(result.Data?.Log)) { return new CliActionResponse(result); }

            var log = CliFormat.GetLogMarkup(result.Data?.Log) ?? string.Empty;
            return new CliActionResponse(result, log, true);
        }
    }

    [Action("running-ex")]
    [NullRequest]
    public static async Task<CliActionResponse> GetRunningExceptions(CliFireInstanceIdRequest request, CancellationToken cancellationToken = default)
    {
        request ??= new CliFireInstanceIdRequest();
        if (string.IsNullOrWhiteSpace(request.FireInstanceId))
        {
            request.FireInstanceId = await ChooseRunningJobInstance();
        }

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
    [Action("run")]
    public static async Task<CliActionResponse> InvokeJob(CliInvokeJobRequest request, CancellationToken cancellationToken = default)
    {
        var result = await InvokeJobInner(request, cancellationToken);
        return new CliActionResponse(result);
    }

    [Action("data")]
    public static async Task<CliActionResponse> JobData(CliJobDataRequest request, CancellationToken cancellationToken = default)
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

            case DataActions.Clear:
                if (!ConfirmAction($"clear all data from job {request.Id}")) { return CliActionResponse.Empty; }

                var restRequest3 = new RestRequest("job/{id}/data", Method.Delete)
                    .AddParameter("id", request.Id, ParameterType.UrlSegment);

                result = await RestProxy.Invoke(restRequest3, cancellationToken);
                break;

            default:
                throw new CliValidationException($"action {request.Action} is not supported for this command");
        }

        AssertJobDataUpdated(result, request.Id);
        return new CliActionResponse(result);
    }

    [Action("pause")]
    public static async Task<CliActionResponse> PauseJob(CliPauseRequest request, CancellationToken cancellationToken = default)
    {
        var autoResumeDate = request.For == null ? (DateTime?)null : DateTime.Now.Add(request.For.Value);
        var restRequest = new RestRequest("job/pause", Method.Post)
            .AddBody(new { request.Id, autoResumeDate });

        var result = await RestProxy.Invoke(restRequest, cancellationToken);
        return new CliActionResponse(result);
    }

    [Action("pause-group")]
    public static async Task<CliActionResponse> PauseJobGroup(CliByNameRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            request.Name = await ChooseGroup(cancellationToken);
        }

        var restRequest = new RestRequest("job/pause-group", Method.Post)
            .AddBody(request);

        var result = await RestProxy.Invoke(restRequest, cancellationToken);
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
    public static async Task<CliActionResponse> ResumeJob(CliResumeRequest request, CancellationToken cancellationToken = default)
    {
        var autoResumeDate = request.In == null ? (DateTime?)null : DateTime.Now.Add(request.In.Value);
        var restRequest = new RestRequest("job/resume", Method.Post)
            .AddBody(new { request.Id, autoResumeDate });

        var result = await RestProxy.Invoke(restRequest, cancellationToken);
        return new CliActionResponse(result);
    }

    [Action("resume-group")]
    public static async Task<CliActionResponse> ResumeJobGroup(CliByNameRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            request.Name = await ChooseGroup(cancellationToken);
        }

        var restRequest = new RestRequest("job/resume-group", Method.Post)
            .AddBody(request);

        var result = await RestProxy.Invoke(restRequest, cancellationToken);
        return new CliActionResponse(result);
    }

    [Action("auto-resume")]
    public static async Task<CliActionResponse> SetAutoResume(CliAutoResumeRequest request, CancellationToken cancellationToken = default)
    {
        CollectCliAutoResumeRequest(request);

        var autoResumeDate = request.In == null ? (DateTime?)null : DateTime.Now.Add(request.In.Value);
        var restRequest = new RestRequest("job/auto-resume", Method.Post)
                .AddBody(new { request.Id, autoResumeDate });

        var result = await RestProxy.Invoke(restRequest, cancellationToken);
        return new CliActionResponse(result);
    }

    [Action("cancel-auto-resume")]
    public static async Task<CliActionResponse> CancelAutoResume(CliJobKey request, CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest("job/{id}/auto-resume", Method.Delete)
            .AddParameter("id", request.Id, ParameterType.UrlSegment);

        var result = await RestProxy.Invoke(restRequest, cancellationToken);
        return new CliActionResponse(result);
    }

    [Action("set-author")]
    [NullRequest]
    public static async Task<CliActionResponse> SetAuthor(CliSetAuthorOfJob request, CancellationToken cancellationToken = default)
    {
        request ??= new CliSetAuthorOfJob
        {
            Id = await ChooseJob(null, false, writeSelection: true, cancellationToken)
        };

        FillRequiredString(request, nameof(request.Author));

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
        var step4 = await TestStep3GetRunningData(instanceId, invokeDate, logId, cancellationToken);
        if (step4 != null) { return step4; }

        // (4) Sleep 1 sec
        await Task.Delay(500, cancellationToken);

        // (5) Check log
        var step6 = await TestStep5CheckLog(logId, cancellationToken);
        if (step6 != null) { return step6; }
        return CliActionResponse.Empty;
    }

    [Action("update")]
    [NullRequest]
    public static async Task<CliActionResponse> UpdateJob(CliUpdateJobRequest request, CancellationToken cancellationToken = default)
    {
        request ??= new CliUpdateJobRequest();
        var body = new UpdateJobRequest { JobFilePath = request.Filename };

        if (Util.IsJobId(request.Filename))
        {
            var filenameRequest = new RestRequest("job/jobfilename/{id}", Method.Get)
                .AddParameter("id", request.Filename, ParameterType.UrlSegment);
            var filenameResult = await RestProxy.Invoke<string>(filenameRequest, cancellationToken);

            if (filenameResult.IsSuccessful && filenameResult.Data != null)
            {
                body.JobFilePath = filenameResult.Data;
            }
            else
            {
                return new CliActionResponse(filenameResult);
            }
        }

        if (string.IsNullOrWhiteSpace(request.Filename))
        {
            var jobsRequest = new RestRequest("job/available-jobs", Method.Get)
                .AddQueryParameter("update", "true");
            var jobsResult = await RestProxy.Invoke<List<AvailableJob>>(jobsRequest, cancellationToken);
            if (!jobsResult.IsSuccessful)
            {
                return new CliActionResponse(jobsResult);
            }

            var filename = SelectJobFilename(jobsResult.Data);
            body.JobFilePath = filename;
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

    [Action("wait")]
    public static async Task<CliActionResponse> Wait(CliJobWaitRequest request, CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient();
        var uri = request.GetQueryParam("job/wait");

        client.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, uri);
        using var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var restResponse = await CliActionResponse.Convert(response);
            return new CliActionResponse(restResponse);
        }

        using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(body);
        ServerSendEvent<WaitEventData> data = new();

        var table = new Table().Border(TableBorder.Rounded).HideHeaders();
        table.AddColumns("Caption", "Value");
        table.AddRow("Estimated End Time", string.Empty);
        table.AddRow("Running Instances", string.Empty);

        await AnsiConsole.Live(table)
            .AutoClear(true)
            .StartAsync(async context =>
            {
                await Task.Yield();
                while (!reader.EndOfStream)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var value = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(value)) { continue; }
                    if (data.Parse(value))
                    {
                        table.UpdateCell(0, 1, CliTableFormat.FormatTimeSpan(data.Data?.EstimatedEndTime));
                        table.UpdateCell(1, 1, CliTableFormat.FormatNumber(data.Data?.TotalRunningInstances));
                        context.Refresh();
                        data = new();
                    }
                }
            });

        return CliActionResponse.Empty;
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
                resultData = [result.Data];
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

        if (items.Count == 0)
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
        var result = await RestProxy.Invoke<List<AvailableJob>>(restRequest, cancellationToken);
        if (!result.IsSuccessful)
        {
            return new RequestBuilderWrapper<CliAddJobRequest> { FailResponse = result };
        }

        var folder = SelectJobFilename(result.Data);
        var request = new CliAddJobRequest { Filename = folder };
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

    private static async Task<RestResponse<JobDetails>> GetJob(string id, CancellationToken cancellationToken)
    {
        var restRequest = new RestRequest("job/{id}", Method.Get)
            .AddParameter("id", id, ParameterType.UrlSegment);

        var result = await RestProxy.Invoke<JobDetails>(restRequest, cancellationToken);
        return result;
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

        restRequest.Timeout = TimeSpan.FromMilliseconds(35_000); // 35 sec

        var result = await RestProxy.Invoke<LastInstanceId>(restRequest, cancellationToken);
        return result;
    }

    private static async Task<(CliActionResponse?, bool, RestResponse<RunningJobDetails>)> InitGetRunningData(string instanceId, CancellationToken cancellationToken)
    {
        var restRequest = new RestRequest("job/running-instance/{instanceId}", Method.Get)
            .AddParameter("instanceId", instanceId, ParameterType.UrlSegment);
        var runResult = await RestProxy.InvokeWithoutSpinner<RunningJobDetails>(restRequest, cancellationToken);

        var counter = 0;
        while (!runResult.IsSuccessful && counter < 3)
        {
            await Task.Delay(500, cancellationToken);
            runResult = await RestProxy.InvokeWithoutSpinner<RunningJobDetails>(restRequest, cancellationToken);
            counter++;
        }

        if (!runResult.IsSuccessful)
        {
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

    // bug fix: test finish while job still running
    private static async Task<bool> IsHistoryStatusRunning(long logId, CancellationToken cancellationToken)
    {
        var restRequest = new RestRequest("history/{id}/status", Method.Get)
            .AddParameter("id", logId, ParameterType.UrlSegment);
        var result = await RestProxy.InvokeWithoutSpinner<int>(restRequest, cancellationToken);
        if (!result.IsSuccessful) { return true; }
        return result.Data == -1;
    }

    private static async Task<(RestResponse<RunningJobDetails>, DateTime?)> LongPollingGetRunningData(
            RestResponse<RunningJobDetails> runResult,
            string instanceId,
            CancellationToken cancellationToken)
    {
        var data = runResult.Data;
        var restRequest = new RestRequest("job/running-instance/{instanceId}/long-polling", Method.Get)
            .AddParameter("instanceId", instanceId, ParameterType.UrlSegment)
            .AddQueryParameter("progress", data?.Progress ?? 0)
            .AddQueryParameter("effectedRows", data?.EffectedRows ?? 0)
            .AddQueryParameter("exceptionsCount", data?.ExceptionsCount ?? 0);

        restRequest.Timeout = TimeSpan.FromMilliseconds(360_000); // 6 min
        var counter = 1;
        while (counter <= 3)
        {
            runResult = await RestProxy.InvokeWithoutSpinner<RunningJobDetails>(restRequest, cancellationToken);
            if (runResult.IsSuccessful) { break; }
            if (runResult.StatusCode == HttpStatusCode.NotFound) { break; }
            if (runResult.StatusCode == HttpStatusCode.BadRequest) { break; }
            await Task.Delay(500 + ((counter - 1) ^ 2) * 500, cancellationToken);
            counter++;
        }

        var estimateEnd = GetEstimatedEndTime(runResult);
        return (runResult, estimateEnd);
    }

    private static UpdateJobOptions MapUpdateJobOptions()
    {
        using var _ = new TokenBlockerScope();
        var options = new List<CliSelectItem<JobUpdateOptions>>
        {
            new() { DisplayName = "without data (default)", Value = JobUpdateOptions.NoData },
            new() { DisplayName = "with job data", Value =  JobUpdateOptions.JobData },
            new() { DisplayName = "with triggers data", Value =  JobUpdateOptions.TriggersData },
            new() { DisplayName = "all data", Value =  JobUpdateOptions.All },
        };

        var selected = CliPromptUtil.PromptSelection(options, "select update options");
        CliPromptUtil.CheckForCancelOption(selected);
        var result = MapUpdateJobOptions(selected?.Value ?? JobUpdateOptions.NoData);
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
                throw new CliValidationException($"option {options} is invalid", "use one or more from the following options: no-data, job-data, triggers-data, all");
        }

        return result;
    }

    private static string SelectJobFilename(IEnumerable<AvailableJob>? data)
    {
        if (data == null || !data.Any())
        {
            throw new CliWarningException("no available jobs found on server");
        }

        var items = data.Select(e =>
            new CliSelectItem<string>
            {
                DisplayName = $"{e.Name.EscapeMarkup()} {CliConsts.GroupDisplayFormat}({e.JobFile.EscapeMarkup()})[/]",
                Value = e.JobFile
            });

        var selectedItem = PromptSelection(items, "job");
        return selectedItem?.Value ?? string.Empty;
    }

    private static string? ShowGroupsMenu(IEnumerable<string> data, bool writeSelection = true)
    {
        var groups = data
            .OrderBy(d => d)
            .Distinct()
            .ToList();

        return PromptSelection(groups, "job group", writeSelection);
    }

    private static string ShowJobsMenu(IEnumerable<JobBasicDetails> data, string? groupName = null, bool writeSelection = true)
    {
        var query = data.AsQueryable();

        if (!string.IsNullOrEmpty(groupName))
        {
            query = query.Where(d => d.Group.Equals(groupName, StringComparison.CurrentCultureIgnoreCase));
        }

        var jobs = query
            .OrderBy(d => d.Group)
            .ThenBy(d => d.Name)
            .Select(d => CliTableFormat.FormatJobKey(d.Group, d.Name))
            .ToList();

        var result = PromptSelection(jobs, "job", writeSelection) ?? string.Empty;
        result = CliTableFormat.UnformatJobName(result);
        return result;
    }

    private static async Task<CliActionResponse?> TestStep0CheckJob(CliInvokeJobRequest request, CancellationToken cancellationToken)
    {
        // (0) Check for running job
        AnsiConsole.MarkupLine(" [gold3_1][[x]][/] Check job…");
        var result = await CheckAlreadyRunningJob(request, cancellationToken);
        if (result.IsSuccessful) { return null; }

        return new CliActionResponse(result);
    }

    private static async Task<CliActionResponse?> TestStep1InvokeJob(CliInvokeJobRequest request, CancellationToken cancellationToken)
    {
        // (1) Invoke job
        AnsiConsole.MarkupLine(" [gold3_1][[x]][/] Invoke job…");
        var result = await InvokeJobInner(request, cancellationToken);
        if (result.IsSuccessful)
        {
            return null;
        }

        return new CliActionResponse(result);
    }

    private static async Task<TestData> TestStep2GetInstanceId(CliInvokeJobRequest request, DateTime invokeDate, CancellationToken cancellationToken)
    {
        AnsiConsole.Markup(" [gold3_1][[x]][/] Get instance id… ");

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

    private static async Task<CliActionResponse?> TestStep3GetRunningData(string instanceId, DateTime invokeDate, long logId, CancellationToken cancellationToken)
    {
        // check for very fast finish job
        var result = await InitGetRunningData(instanceId, cancellationToken);
        if (result.Item2) { return result.Item1; }

        Task dataTask = Task.CompletedTask;
        var runResult = result.Item3;
        DateTime? estimateEnd = null;

        var table = GetRunningTable(runResult, invokeDate, estimateEnd);
        if (table == null)
        {
            var isRunning = await IsHistoryStatusRunning(logId, cancellationToken);
            if (!isRunning) { return null; }
            table = new Table();
        }

        await AnsiConsole.Live(table)
            .AutoClear(true)
            .StartAsync(async ctx =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var success = UpdateRunningTable(table, ctx, runResult, invokeDate, estimateEnd);
                    if (!success)
                    {
                        var isRunning = await IsHistoryStatusRunning(logId, cancellationToken);
                        if (!isRunning) { break; }
                    }

                    if (dataTask.Status == TaskStatus.RanToCompletion)
                    {
                        dataTask = Task.Run(async () =>
                        {
                            var (result, estimateEnd) = await LongPollingGetRunningData(runResult, instanceId, cancellationToken);
                            runResult = result;
                            UpdateRunningTable(table, ctx, runResult, invokeDate, estimateEnd);
                        }, cancellationToken);
                    }

                    // wait for 1 sec then break and write running data on screen
                    for (int i = 0; i < 5; i++)
                    {
                        await Task.Delay(200, cancellationToken);
                        if (dataTask.Status == TaskStatus.RanToCompletion) { break; }
                    }

                    if (!runResult.IsSuccessful)
                    {
                        var isRunning = await IsHistoryStatusRunning(logId, cancellationToken);
                        if (!isRunning) { break; }
                    }
                }
            });

        return null;
    }

    private static async Task<CliActionResponse?> TestStep5CheckLog(long logId, CancellationToken cancellationToken)
    {
        var restRequest = new RestRequest("odata/HistoryData", Method.Get)
           .AddQueryParameter("filter", $"Id eq {logId}")
           .AddQueryParameter("select", "Duration,EffectedRows,ExceptionCount,Status");

        var result = await RestProxy.InvokeWithoutSpinner<JobHistoryOdataWrapper>(restRequest, cancellationToken);
        var data = result.Data?.Value?.FirstOrDefault();
        if (result.StatusCode == HttpStatusCode.NotFound || data == null)
        {
            Console.WriteLine();
            throw new CliException($"could not found log data for log id {logId}");
        }

        if (!result.IsSuccessful) { return new CliActionResponse(result); }

        var table = GetRunningTable(data, data.Duration.GetValueOrDefault());
        AnsiConsole.Write(table);
        if (data.Status == 0)
        {
            AnsiConsole.MarkupLine("[black on green] Success [/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[black on red] Fail [/]");
        }

        Console.WriteLine();

        table = new Table();
        table.AddColumn(new TableColumn(new Markup("[grey54]Get more information by the following commands[/]")));
        table.BorderColor(Color.FromInt32(242));
        table.AddRow($"[grey54]history get[/] [grey62]{logId}[/]");
        table.AddRow($"[grey54]history log[/] [grey62]{logId}[/]");
        table.AddRow($"[grey54]history data[/] [grey62]{logId}[/]");

        if (data.Status == 1)
        {
            table.AddRow($"[grey54]history ex[/] [grey62]{logId}[/]");
        }

        AnsiConsole.Write(table);

        return null;
    }

    private static bool UpdateRunningTable(Table table, LiveDisplayContext context, RestResponse<RunningJobDetails> runResult, DateTime invokeDate, DateTime? estimateEnd)
    {
        if (runResult.Data == null) { return false; }
        var data = runResult.Data;
        var span = DateTimeOffset.Now.Subtract(invokeDate);
        var endSpan = estimateEnd == null ? data.EstimatedEndTime : estimateEnd.Value.Subtract(DateTime.Now);
        table.UpdateCell(0, 0, CreateProgressBarMarkup(data.Progress));
        table.UpdateCell(0, 1, $"[gray]{CliTableFormat.FormatNumber(data.EffectedRows)}[/]");
        table.UpdateCell(0, 2, CliTableFormat.FormatExceptionCount(data.ExceptionsCount));
        table.UpdateCell(0, 3, $"[gray]{CliTableFormat.FormatTimeSpan(span)}[/]");
        table.UpdateCell(0, 4, $"[gray]{CliTableFormat.FormatTimeSpan(endSpan)}[/]");
        context.Refresh();
        return true;
    }

    /// <summary>
    /// Helper method to create a simple text-based progress bar representation using Markup.
    /// You could also use a custom renderable that visually looks more like Spectre's standard progress bar.
    /// </summary>
    private static Markup CreateProgressBarMarkup(int percentage, Color? color = null)
    {
        color ??= Color.Gold3_1;
        int completedChars = percentage / 5; // Each char is 5%
        int remainingChars = 20 - completedChars;

        // Build the bar: [▬▬▬▬▬▬▬▬▬▬       ] 50%
        var bar = new StringBuilder();
        bar.Append('▬', completedChars);
        bar.Append(' ', remainingChars);
        var space = percentage < 10 ? "  " : (percentage < 100 ? " " : string.Empty);
        var final = $"[{color.Value}][[{bar}]][/] {percentage}%{space}";
        return new Markup(final);
    }

    private static Table? GetRunningTable(RestResponse<RunningJobDetails> runResult, DateTime invokeDate, DateTime? estimateEnd)
    {
        if (runResult.Data == null) { return null; }
        var data = runResult.Data;
        var span = DateTimeOffset.Now.Subtract(invokeDate);
        var endSpan = estimateEnd == null ? data.EstimatedEndTime : estimateEnd.Value.Subtract(DateTime.Now);

        var table = new Table();
        table.AddColumn("Progress", col => col.Centered());
        table.AddColumn("Effected Row(s)", col => col.Centered());
        table.AddColumn("Exception Count", col => col.Centered());
        table.AddColumn("Run Time", col => col.Centered());
        table.AddColumn("End Time");
        table.AddRow(
            $"[gray]{data.Progress}%[/]",
            $"[gray]{CliTableFormat.FormatNumber(data.EffectedRows)}[/]",
            CliTableFormat.FormatExceptionCount(data.ExceptionsCount),
            $"[gray]{CliTableFormat.FormatTimeSpan(span)}[/]",
            $"[gray]{CliTableFormat.FormatTimeSpan(endSpan)}[/]");

        return table;
    }

    private static Table GetRunningTable(JobHistory data, int duration)
    {
        var span = TimeSpan.FromMilliseconds(duration);
        var table = new Table();
        table.AddColumn("Progress", col => col.Centered());
        table.AddColumn("Effected Row(s)", col => col.Centered());
        table.AddColumn("Exception Count", col => col.Centered());
        table.AddColumn("Run Time", col => col.Centered());
        table.AddColumn("End Time");

        table.AddRow(
            "[gray]100%[/]",
            $"[gray]{CliTableFormat.FormatNumber(data.EffectedRows)}[/]",
            CliTableFormat.FormatExceptionCount(data.ExceptionCount),
            $"[gray]{CliTableFormat.FormatTimeSpan(span)}[/]",
            "[gray]--:--:--[/]");

        return table;
    }

    private static void CollectCliAutoResumeRequest(CliAutoResumeRequest request)
    {
        if (request.In.GetValueOrDefault() == TimeSpan.Zero)
        {
            var ts = CliPromptUtil.PromptForTimeSpan("resume in:", required: true);
            request.In = ts ?? TimeSpan.Zero;
        }
    }

    private struct TestData
    {
        public string InstanceId { get; set; }
        public long LogId { get; set; }
        public CliActionResponse? Response { get; set; }
    }
}
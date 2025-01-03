﻿using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using Planar.CLI.Exceptions;
using Planar.CLI.Proxy;
using RestSharp;
using Spectre.Console;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Actions;

[Module("trigger", "Actions to add, remove, list, update and operate triggers of job", Synonyms = "triggers")]
public class TriggerCliActions : BaseCliAction<TriggerCliActions>
{
    [Action("ls")]
    [Action("list")]
    public static async Task<CliActionResponse> GetTriggersDetails(CliListTriggersRequest request, CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest("trigger/{jobId}/by-job", Method.Get);

        if (!string.IsNullOrWhiteSpace(request.Id))
        {
            restRequest.AddParameter("jobId", request.Id, ParameterType.UrlSegment);
        }

        var result = await RestProxy.Invoke<TriggerRowDetails>(restRequest, cancellationToken);
        CliActionResponse response = new(result);

        if (!result.IsSuccessful || result.Data == null)
        {
            return response;
        }

        var message = string.Empty;
        if (request.Quiet)
        {
            var all = result.Data.SimpleTriggers
                .Select(t => t.Id)
                .Union(result.Data.CronTriggers.Select(c => c.Id));

            message = string.Join('\n', all);
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
    public static async Task<CliActionResponse> GetTriggerDetails(CliGetTriggersDetailsRequest request, CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest("trigger/{triggerId}", Method.Get)
            .AddParameter("triggerId", request.Id, ParameterType.UrlSegment);

        var result = await RestProxy.Invoke<TriggerRowDetails>(restRequest, cancellationToken);

        if (!result.IsSuccessful || result.Data == null)
        {
            CliActionResponse response = new(result);
            return response;
        }

        if (result.Data.SimpleTriggers.Count != 0)
        {
            return new CliActionResponse(result, dumpObject: result.Data.SimpleTriggers[0]);
        }

        if (result.Data.CronTriggers.Count != 0)
        {
            return new CliActionResponse(result, dumpObject: result.Data.CronTriggers[0]);
        }

        return new CliActionResponse(result);
    }

    [Action("remove")]
    [Action("delete")]
    public static async Task<CliActionResponse> RemoveTrigger(CliTriggerKey request, CancellationToken cancellationToken = default)
    {
        if (!ConfirmAction($"remove trigger id {request.Id}")) { return CliActionResponse.Empty; }

        var restRequest = new RestRequest("trigger/{triggerId}", Method.Delete)
            .AddParameter("triggerId", request.Id, ParameterType.UrlSegment);

        return await Execute(restRequest, cancellationToken);
    }

    [Action("pause")]
    public static async Task<CliActionResponse> PauseTrigger(CliTriggerKey request, CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest("trigger/pause", Method.Post)
            .AddBody(request);

        return await Execute(restRequest, cancellationToken);
    }

    [Action("resume")]
    public static async Task<CliActionResponse> ResumeTrigger(CliTriggerKey request, CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest("trigger/resume", Method.Post)
            .AddBody(request);

        return await Execute(restRequest, cancellationToken);
    }

    [Action("set-cronexpr")]
    public static async Task<CliActionResponse> UpdateCron(CliUpdateCronRequest request, CancellationToken cancellationToken = default)
    {
        FillRequiredString(request, nameof(request.CronExpression));
        var restRequest = new RestRequest("trigger/cron-expression", Method.Patch)
            .AddBody(request);

        return await Execute(restRequest, cancellationToken);
    }

    [Action("set-interval")]
    public static async Task<CliActionResponse> UpdateInterval(CliUpdateIntervalRequest request, CancellationToken cancellationToken = default)
    {
        FillRequiredTimeSpan(request, nameof(request.Interval));
        var restRequest = new RestRequest("trigger/interval", Method.Patch)
            .AddBody(request);

        return await Execute(restRequest, cancellationToken);
    }

    [Action("set-timeout")]
    public static async Task<CliActionResponse> UpdateTimeout(CliUpdateTimeoutRequest request, CancellationToken cancellationToken = default)
    {
        FillRequiredTimeSpan(request, nameof(request.Timeout));

        var restRequest = new RestRequest("trigger/timeout", Method.Patch)
            .AddBody(request);

        return await Execute(restRequest, cancellationToken);
    }

    [Action("clear-timeout")]
    public static async Task<CliActionResponse> ClearTimeout(CliTriggerKey request, CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest("trigger/timeout", Method.Patch)
            .AddBody(request);

        return await Execute(restRequest, cancellationToken);
    }

    [Action("cronexpr")]
    public static async Task<CliActionResponse> GetCronExpression(CliCronExpression request, CancellationToken cancellationToken = default)
    {
        FillRequiredString(request, nameof(request.Expression));

        var restRequest = new RestRequest("trigger/cron", Method.Get)
            .AddQueryParameter("expression", request.Expression);

        var result = await RestProxy.Invoke<string>(restRequest, cancellationToken);
        if (result.IsSuccessful)
        {
            return new CliActionResponse(result, message: result.Data);
        }

        return new CliActionResponse(result);
    }

    [Action("paused")]
    public static async Task<CliActionResponse> GetAllPausedTriggers(CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest("trigger/paused", Method.Get);
        var result = await RestProxy.Invoke<List<PausedTriggerDetails>>(restRequest, cancellationToken);

        var table = CliTableExtensions.GetTable(result.Data);
        return new CliActionResponse(result, table);
    }

    [Action("data")]
    public static async Task<CliActionResponse> PutTriggerData(CliTriggerDataRequest request, CancellationToken cancellationToken = default)
    {
        FillMissingDataProperties(request);
        RestResponse result;
        switch (request.Action)
        {
            case DataActions.Put:
                var prm1 = new JobOrTriggerDataRequest
                {
                    Id = request.Id,
                    DataKey = request.DataKey
                };

                if (request.DataValue != null)
                {
                    prm1.DataValue = request.DataValue;
                }

                var restRequest1 = new RestRequest("trigger/data", Method.Post).AddBody(prm1);
                result = await RestProxy.Invoke(restRequest1, cancellationToken);

                if (result.StatusCode == HttpStatusCode.Conflict)
                {
                    restRequest1 = new RestRequest("trigger/data", Method.Put).AddBody(prm1);
                    result = await RestProxy.Invoke(restRequest1, cancellationToken);
                }
                break;

            case DataActions.Remove:
                if (!ConfirmAction($"remove data with key '{request.DataKey}' from trigger {request.Id}")) { return CliActionResponse.Empty; }
                var restRequest2 = new RestRequest("trigger/{id}/data/{key}", Method.Delete)
                    .AddParameter("id", request.Id, ParameterType.UrlSegment)
                    .AddParameter("key", request.DataKey, ParameterType.UrlSegment);

                result = await RestProxy.Invoke(restRequest2, cancellationToken);
                break;

            case DataActions.Clear:
                if (!ConfirmAction($"clear all data from trigger {request.Id}")) { return CliActionResponse.Empty; }
                var restRequest3 = new RestRequest("trigger/{id}/data", Method.Delete)
                    .AddParameter("id", request.Id, ParameterType.UrlSegment);

                result = await RestProxy.Invoke(restRequest3, cancellationToken);
                break;

            default:
                throw new CliValidationException($"action {request.Action} is not supported for this command");
        }

        AssertTriggerUpdated(result, request.Id);
        return new CliActionResponse(result);
    }
}
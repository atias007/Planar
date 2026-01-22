using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using Planar.CLI.Proxy;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Actions;

[Module("config", "add, remove, list & update global parameters", Synonyms = "configs")]
public class ConfigCliActions : BaseCliAction<ConfigCliActions>
{
    [Action("get")]
    public static async Task<CliActionResponse> GetConfig(CliConfigKeyRequest request, CancellationToken cancellationToken = default)
    {
        FillRequiredString(request, nameof(request.Key));

        var restRequest = new RestRequest("config/{key}", Method.Get)
            .AddParameter("key", request.Key, ParameterType.UrlSegment);
        var result = await RestProxy.Invoke<CliGlobalConfig>(restRequest, cancellationToken);

        return new CliActionResponse(result, message: result.Data?.Value);
    }

    [Action("ls")]
    [Action("list")]
    public static async Task<CliActionResponse> GetAllConfiguration(CliListConfigsRequest request, CancellationToken cancellationToken = default)
    {
        RestRequest restRequest;

        if (request.Flat)
        {
            restRequest = new RestRequest("config/flat", Method.Get);
            return await ExecuteTable<List<KeyValueItem>>(restRequest, CliTableExtensions.GetTable, cancellationToken);
        }
        else
        {
            restRequest = new RestRequest("config", Method.Get);
            return await ExecuteTable<List<CliGlobalConfig>>(restRequest, CliTableExtensions.GetTable, cancellationToken);
        }
    }

    [Action("add")]
    public static async Task<CliActionResponse> Add(CliConfigRequest request, CancellationToken cancellationToken = default)
    {
        FillRequiredString(request, nameof(request.Key));
        FillOptionalString(request, nameof(request.Value));

        var data = new { request.Key, request.Value, request.SourceUrl };
        var restRequest = new RestRequest("config", Method.Post)
            .AddBody(data);

        var result = await RestProxy.Invoke(restRequest, cancellationToken);
        return new CliActionResponse(result);
    }

    [Action("load-yml")]
    public static async Task<CliActionResponse> LoadYml(CliConfigFileRequest request, CancellationToken cancellationToken = default)
    {
        FillRequiredString(request, nameof(request.Key));
        FillRequiredString(request, nameof(request.Filename));

        return await LoadConfig(request, GlobalConfigTypes.Yml, cancellationToken);
    }

    [Action("load-json")]
    public static async Task<CliActionResponse> LoadJson(CliConfigFileRequest request, CancellationToken cancellationToken = default)
    {
        FillRequiredString(request, nameof(request.Key));
        FillRequiredString(request, nameof(request.Filename));

        return await LoadConfig(request, GlobalConfigTypes.Json, cancellationToken);
    }

    [Action("update")]
    public static async Task<CliActionResponse> Update(CliConfigRequest request, CancellationToken cancellationToken = default)
    {
        FillRequiredString(request, nameof(request.Key));
        FillOptionalString(request, nameof(request.Value));
        FillOptionalString(request, nameof(request.SourceUrl));

        var data = new { request.Key, request.Value, request.SourceUrl };
        var restRequest = new RestRequest("config", Method.Put)
            .AddBody(data);

        var result = await RestProxy.Invoke(restRequest, cancellationToken);
        return new CliActionResponse(result);
    }

    [Action("flush")]
    public static async Task<CliActionResponse> Flush(CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest("config/flush", Method.Post);
        return await Execute(restRequest, cancellationToken);
    }

    [Action("remove")]
    [Action("delete")]
    public static async Task<CliActionResponse> RemoveConfig(CliConfigKeyRequest request, CancellationToken cancellationToken = default)
    {
        FillRequiredString(request, nameof(request.Key));

        if (!ConfirmAction($"remove config '{request.Key}'")) { return CliActionResponse.Empty; }

        var restRequest = new RestRequest("config/{key}", Method.Delete)
            .AddParameter("key", request.Key, ParameterType.UrlSegment);
        return await Execute(restRequest, cancellationToken);
    }

    private static async Task<CliActionResponse> LoadConfig(CliConfigFileRequest request, GlobalConfigTypes configType, CancellationToken cancellationToken)
    {
        ValidateFileExists(request.Filename);
        var value = await File.ReadAllTextAsync(request.Filename, cancellationToken);
        var type = configType.ToString().ToLower();

        var data = new { request.Key, value, Type = type };
        var restRequest = new RestRequest("config", Method.Put)
            .AddBody(data);

        var result = await RestProxy.Invoke(restRequest, cancellationToken);
        if (result.StatusCode == HttpStatusCode.NotFound)
        {
            restRequest.Method = Method.Post;
            result = await RestProxy.Invoke(restRequest, cancellationToken);
        }

        return new CliActionResponse(result);
    }
}
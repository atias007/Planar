using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.CliGeneral;
using Planar.CLI.Entities;
using Planar.CLI.Proxy;
using RestSharp;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Actions;

[Module("config", "add, remove, list & update global parameters", Synonyms = "configs")]
public class ConfigCliActions : BaseCliAction<ConfigCliActions>
{
    private const string c_config = "config";

    [Action("apply")]
    public static async Task<CliActionResponse> Apply(CliApplyRequest request, CancellationToken cancellationToken = default)
    {
        return await Apply(c_config, request, cancellationToken);
    }

    [Action("add")]
    public static async Task<CliActionResponse> Add(CliAddConfigRequest request, CancellationToken cancellationToken = default)
    {
        var wrapper = FillCliAddConfigRequest(request);
        if (!wrapper.IsSuccessful)
        {
            return new CliActionResponse(wrapper.FailResponse);
        }

        var data = new { request.Key, request.Value, IsSecret = false };
        var restRequest = new RestRequest(c_config, Method.Post)
            .AddBody(data);

        var result = await RestProxy.Invoke(restRequest, cancellationToken);
        return new CliActionResponse(result);
    }

    [Action("convert-to-secret")]
    public static async Task<CliActionResponse> ConvertToSecret(CliConfigKeyRequest request, CancellationToken cancellationToken = default)
    {
        var wrapper = await FillCliConfigKeyRequest(request, cancellationToken);
        if (!wrapper.IsSuccessful)
        {
            return new CliActionResponse(wrapper.FailResponse);
        }

        var restRequest = new RestRequest("config/{key}/convert-to-secret", Method.Patch)
            .AddUrlSegment("key", request.Key);
        var result = await RestProxy.Invoke(restRequest, cancellationToken);
        return new CliActionResponse(result);
    }

    [Action("add-secret")]
    public static async Task<CliActionResponse> AddSecret(CliAddConfigRequest request, CancellationToken cancellationToken = default)
    {
        var wrapper = FillCliAddConfigRequest(request);
        if (!wrapper.IsSuccessful)
        {
            return new CliActionResponse(wrapper.FailResponse);
        }

        var data = new { request.Key, request.Value, IsSecret = true };
        var restRequest = new RestRequest(c_config, Method.Post)
            .AddBody(data);

        var result = await RestProxy.Invoke(restRequest, cancellationToken);
        return new CliActionResponse(result);
    }

    [Action("add-url")]
    public static async Task<CliActionResponse> AddUrl(CliAddUrlConfigRequest request, CancellationToken cancellationToken = default)
    {
        var wrapper = FillCliAddUrlConfigRequest(request);
        if (!wrapper.IsSuccessful)
        {
            return new CliActionResponse(wrapper.FailResponse);
        }

        var data = new { request.Key, request.SourceUrl, IsSecret = false, Type = request.Type?.ToString().ToLower() };
        var restRequest = new RestRequest(c_config, Method.Post)
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

    [Action("ls")]
    [Action("list")]
    public static async Task<CliActionResponse> GetAllConfiguration(CliListConfigsRequest request, CancellationToken cancellationToken = default)
    {
        RestRequest restRequest;

        if (request.Flat)
        {
            restRequest = new RestRequest("config/flat", Method.Get)
                .AddQueryPagingParameter(request);
            return await ExecuteTable<PagingResponse<KeyValueItem>>(restRequest, CliTableExtensions.GetTable, cancellationToken);
        }
        else
        {
            restRequest = new RestRequest(c_config, Method.Get)
                .AddQueryPagingParameter(request);
            return await ExecuteTable<PagingResponse<CliGlobalConfig>>(restRequest, CliTableExtensions.GetTable, cancellationToken);
        }
    }

    [Action("get")]
    public static async Task<CliActionResponse> GetConfig(CliConfigKeyRequest request, CancellationToken cancellationToken = default)
    {
        var wrapper = await FillCliConfigKeyRequest(request, cancellationToken);
        if (!wrapper.IsSuccessful)
        {
            return new CliActionResponse(wrapper.FailResponse);
        }

        var restRequest = new RestRequest("config/{key}", Method.Get)
            .AddParameter("key", request.Key, ParameterType.UrlSegment);
        var result = await RestProxy.Invoke<CliGlobalConfig>(restRequest, cancellationToken);
        ValidateNotFound(result, request.Key);
        if (result.Data?.IsSecret ?? false)
        {
            throw new CliWarningException($"key '{request.Key}' is a secret and can't be displayed");
        }

        return new CliActionResponse(result, message: result.Data?.Value);
    }

    [Action("load-file")]
    public static async Task<CliActionResponse> LoadJson(CliConfigFileRequest request, CancellationToken cancellationToken = default)
    {
        FillRequiredString(request, nameof(request.Key), 3, 50);
        FillRequiredString(request, nameof(request.Filename), 1, 500);

        var type = request.Type ?? GlobalConfigTypes.String;

        return await LoadConfig(request, type, cancellationToken);
    }

    [Action("remove")]
    [Action("delete")]
    public static async Task<CliActionResponse> RemoveConfig(CliConfigKeyRequest request, CancellationToken cancellationToken = default)
    {
        var wrapper = await FillCliConfigKeyRequest(request, cancellationToken);
        if (!wrapper.IsSuccessful)
        {
            return new CliActionResponse(wrapper.FailResponse);
        }
        if (!ConfirmAction($"remove config '{request.Key}'")) { return CliActionResponse.Empty; }

        var restRequest = new RestRequest("config/{key}", Method.Delete)
            .AddParameter("key", request.Key, ParameterType.UrlSegment);

        var result = await RestProxy.Invoke(restRequest, cancellationToken);
        ValidateNotFound(result, request.Key);
        return new CliActionResponse(result);
    }

    [Action("update")]
    public static async Task<CliActionResponse> UpdateValue(CliUpdateValueConfigRequest request, CancellationToken cancellationToken = default)
    {
        var wrapper = await FillCliUpdateValueConfigRequest(request, cancellationToken);
        if (!wrapper.IsSuccessful)
        {
            return new CliActionResponse(wrapper.FailResponse);
        }

        var exists = await GetConfig(request.Key, cancellationToken);
        if(!exists.IsSuccessful || exists.Data == null) { return new CliActionResponse(exists); }
        if(!string.IsNullOrWhiteSpace(exists.Data.SourceUrl))
        {
            throw new CliException($"key '{request.Key}' has value from url '{exists.Data.SourceUrl}' and can't be update");
        }

        var data = new { request.Key, request.Value };
        var restRequest = new RestRequest(c_config, Method.Put)
            .AddBody(data);

        var result = await RestProxy.Invoke(restRequest, cancellationToken);
        ValidateNotFound(result, request.Key);
        return new CliActionResponse(result);
    }

    [Action("update-url")]
    public static async Task<CliActionResponse> UpdateUrl(CliUpdateUrlConfigRequest request, CancellationToken cancellationToken = default)
    {
        var wrapper = await FillCliUpdateUrlConfigRequest(request, cancellationToken);
        if (!wrapper.IsSuccessful)
        {
            return new CliActionResponse(wrapper.FailResponse);
        }

        var exists = await GetConfig(request.Key, cancellationToken);
        if (!exists.IsSuccessful || exists.Data == null) { return new CliActionResponse(exists); }
        if (string.IsNullOrWhiteSpace(exists.Data.SourceUrl))
        {
            throw new CliException($"key '{request.Key}' has no url value to be update");
        }

        var data = new { request.Key, sourceUrl = request.Url };
        var restRequest = new RestRequest(c_config, Method.Put)
            .AddBody(data);

        var result = await RestProxy.Invoke(restRequest, cancellationToken);
        ValidateNotFound(result, request.Key);
        return new CliActionResponse(result);
    }

    private static void ValidateNotFound(RestResponse response, string key)
    {
        if (response.StatusCode != HttpStatusCode.NotFound) { return; }
        throw new CliException($"key '{key}' was not found");
    }

    private static async Task<CliPromptWrapper> FillCliConfigKeyRequest(CliConfigKeyRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Key))
        {
            var p1 = await CliPromptUtil.GlobalConfigs(cancellationToken);
            if (!p1.IsSuccessful) { return p1; }
            request.Key = p1.Value ?? string.Empty;
        }

        return CliPromptWrapper.Success;
    }

    private static CliPromptWrapper FillCliAddConfigRequest(CliAddConfigRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Key))
        {
            FillRequiredString(request, nameof(request.Key), 3, 50);
        }

        if (string.IsNullOrWhiteSpace(request.Value))
        {
            FillOptionalString(request, nameof(request.Value), 4_000);
        }

        if (string.IsNullOrWhiteSpace(request.Value)) { request.Value = null; }

        return CliPromptWrapper.Success;
    }

    private static CliPromptWrapper FillCliAddUrlConfigRequest(CliAddUrlConfigRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Key))
        {
            FillRequiredString(request, nameof(request.Key), 3, 50);
        }

        if (string.IsNullOrWhiteSpace(request.SourceUrl))
        {
            FillOptionalString(request, nameof(request.SourceUrl), 1_000);
        }

        if (string.IsNullOrWhiteSpace(request.SourceUrl)) { request.SourceUrl = null; }

        return CliPromptWrapper.Success;
    }

    private static async Task<CliPromptWrapper> FillCliUpdateValueConfigRequest(CliUpdateValueConfigRequest request, CancellationToken cancellationToken)
    {
        // Key
        var response = await FillCliConfigKeyRequest(request, cancellationToken);
        if (!response.IsSuccessful) { return response; }

        if (string.IsNullOrWhiteSpace(request.Value))
        {
            FillRequiredString(request, nameof(request.Value), 1, 4_000);
        }

        if (string.IsNullOrWhiteSpace(request.Value)) { request.Value = null; }

        return CliPromptWrapper.Success;
    }

    private static async Task<CliPromptWrapper> FillCliUpdateUrlConfigRequest(CliUpdateUrlConfigRequest request, CancellationToken cancellationToken)
    {
        // Key
        var response = await FillCliConfigKeyRequest(request, cancellationToken);
        if (!response.IsSuccessful) { return response; }

        if (string.IsNullOrWhiteSpace(request.Url))
        {
            FillRequiredString(request, nameof(request.Url), 1, 1_000);
        }

        if (string.IsNullOrWhiteSpace(request.Url)) { request.Url = null; }

        return CliPromptWrapper.Success;
    }

    private static async Task<CliActionResponse> LoadConfig(CliConfigFileRequest request, GlobalConfigTypes configType, CancellationToken cancellationToken)
    {
        ValidateFileExists(request.Filename);
        ValidateFileSize(request.Filename);
        ValidateTextFile(request.Filename);
        var value = await File.ReadAllTextAsync(request.Filename, cancellationToken);
        var type = configType.ToString().ToLower();

        var data = new { request.Key, value, Type = type };
        var restRequest = new RestRequest(c_config, Method.Put)
            .AddBody(data);

        var result = await RestProxy.Invoke(restRequest, cancellationToken);
        if (result.StatusCode == HttpStatusCode.NotFound)
        {
            restRequest.Method = Method.Post;
            result = await RestProxy.Invoke(restRequest, cancellationToken);
        }

        return new CliActionResponse(result);
    }

    private static async Task<RestResponse<CliGlobalConfig>> GetConfig(string key, CancellationToken cancellationToken)
    {
        var restRequest = new RestRequest("config/{key}", Method.Get)
            .AddParameter("key", key, ParameterType.UrlSegment);
        var result = await RestProxy.Invoke<CliGlobalConfig>(restRequest, cancellationToken);
        return result;
    }
}
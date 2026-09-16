using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.CliGeneral;
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
    [Action("add")]
    public static async Task<CliActionResponse> Add(CliAddConfigRequest request, CancellationToken cancellationToken = default)
    {
        var wrapper = FillCliAddConfigRequest(request);
        if (!wrapper.IsSuccessful)
        {
            return new CliActionResponse(wrapper.FailResponse);
        }

        var data = new { request.Key, request.Value, IsSecret = false };
        var restRequest = new RestRequest("config", Method.Post)
            .AddBody(data);

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
        var restRequest = new RestRequest("config", Method.Post)
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

        var data = new { request.Key, request.SourceUrl, IsSecret = false };
        var restRequest = new RestRequest("config", Method.Post)
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
            restRequest = new RestRequest("config/flat", Method.Get);
            return await ExecuteTable<List<KeyValueItem>>(restRequest, CliTableExtensions.GetTable, cancellationToken);
        }
        else
        {
            restRequest = new RestRequest("config", Method.Get);
            return await ExecuteTable<List<CliGlobalConfig>>(restRequest, CliTableExtensions.GetTable, cancellationToken);
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

        return new CliActionResponse(result, message: result.Data?.Value);
    }

    [Action("load-json")]
    public static async Task<CliActionResponse> LoadJson(CliConfigFileRequest request, CancellationToken cancellationToken = default)
    {
        FillRequiredString(request, nameof(request.Key));
        FillRequiredString(request, nameof(request.Filename));

        return await LoadConfig(request, GlobalConfigTypes.Json, cancellationToken);
    }

    [Action("load-yml")]
    public static async Task<CliActionResponse> LoadYml(CliConfigFileRequest request, CancellationToken cancellationToken = default)
    {
        FillRequiredString(request, nameof(request.Key));
        FillRequiredString(request, nameof(request.Filename));

        return await LoadConfig(request, GlobalConfigTypes.Yml, cancellationToken);
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
        return await Execute(restRequest, cancellationToken);
    }

    [Action("update")]
    public static async Task<CliActionResponse> Update(CliUpdateConfigRequest request, CancellationToken cancellationToken = default)
    {
        var wrapper = await FillCliUpdateConfigRequest(request, cancellationToken);
        if (!wrapper.IsSuccessful)
        {
            return new CliActionResponse(wrapper.FailResponse);
        }

        var data = new { request.Key, request.Value, request.SourceUrl };
        var restRequest = new RestRequest("config", Method.Put)
            .AddBody(data);

        var result = await RestProxy.Invoke(restRequest, cancellationToken);
        return new CliActionResponse(result);
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
            FillRequiredString(request, nameof(request.Key));
        }

        if (string.IsNullOrWhiteSpace(request.Value))
        {
            FillOptionalString(request, nameof(request.Value));
        }

        if (string.IsNullOrWhiteSpace(request.Value)) { request.Value = null; }

        return CliPromptWrapper.Success;
    }

    private static CliPromptWrapper FillCliAddUrlConfigRequest(CliAddUrlConfigRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Key))
        {
            FillRequiredString(request, nameof(request.Key));
        }

        if (string.IsNullOrWhiteSpace(request.SourceUrl))
        {
            FillOptionalString(request, nameof(request.SourceUrl));
        }

        if (string.IsNullOrWhiteSpace(request.SourceUrl)) { request.SourceUrl = null; }

        return CliPromptWrapper.Success;
    }

    private static async Task<CliPromptWrapper> FillCliUpdateConfigRequest(CliUpdateConfigRequest request, CancellationToken cancellationToken)
    {
        RestResponse<CliGlobalConfig> result;

        // Key
        var response = await FillCliConfigKeyRequest(request, cancellationToken);
        if (!response.IsSuccessful) { return response; }


        // Get db config
        try
        {
            var restRequest = new RestRequest("config/{key}", Method.Get)
                .AddParameter("key", request.Key, ParameterType.UrlSegment);
            result = await RestProxy.Invoke<CliGlobalConfig>(restRequest, cancellationToken);
            if (!result.IsSuccessful || result.Data == null) { return new CliPromptWrapper<string>(result); }
        }
        catch (Exception ex)
        {
            throw new CliException($"fail to get data for config key '{request.Key}'. {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(result.Data.SourceUrl)) // this is value config
        {
            if (string.IsNullOrWhiteSpace(request.Value))
            {
                var currentValue = result.Data.Value ?? string.Empty;
                var defaultValue = currentValue.Length > 50 ? currentValue[..50] : currentValue;
                FillRequiredString(request, nameof(request.Value), defaultValue);
            }
        }
        else // this is url config
        {
            if (string.IsNullOrWhiteSpace(request.SourceUrl))
            {
                FillRequiredString(request, nameof(request.SourceUrl));
            }
        }

        if (string.IsNullOrWhiteSpace(request.Value)) { request.Value = null; }
        if (string.IsNullOrWhiteSpace(request.SourceUrl)) { request.SourceUrl = null; }

        return CliPromptWrapper.Success;
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
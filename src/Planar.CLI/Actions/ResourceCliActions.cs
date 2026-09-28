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

[Module("resource", "handle resources", Synonyms = "resources")]
public class ResourceCliActions : BaseCliAction<ResourceCliActions>
{
    private const string c_resource = "resource";

    [Action("apply")]
    public static async Task<CliActionResponse> Apply(CliApplyRequest request, CancellationToken cancellationToken = default)
    {
        return await Apply(c_resource, request, cancellationToken);
    }

    [Action("load-file")]
    [NullRequest]
    public static async Task<CliActionResponse> LoadResourceFile(CliLoadResourceFileRequest request, CancellationToken cancellationToken = default)
    {
        request ??= new CliLoadResourceFileRequest();
        FillRequiredString(request, nameof(request.Name));
        FillRequiredString(request, nameof(request.File));
        return await LoadResource(request, cancellationToken);
    }

    [Action("get")]
    [NullRequest]
    public static async Task<CliActionResponse> GetResourceByName(CliByNameRequest request, CancellationToken cancellationToken = default)
    {
        request ??= new CliByNameRequest();
        var wrapper = await FillGetRequest(request, cancellationToken);
        if (!wrapper.IsSuccessful)
        {
            return new CliActionResponse(wrapper.FailResponse);
        }

        var restRequest = new RestRequest("resource/{name}", Method.Get)
            .AddParameter("name", request.Name ?? string.Empty, ParameterType.UrlSegment);

        return await ExecuteEntity<ResourceDetails>(restRequest, cancellationToken);
    }

    [Action("ls")]
    [Action("list")]
    public static async Task<CliActionResponse> GetUsers(CliPagingRequest request, CancellationToken cancellationToken = default)
    {
        var restRequest = new RestRequest(c_resource, Method.Get)
            .AddQueryPagingParameter(request);

        return await ExecuteTable<PagingResponse<ResourceDetails>>(restRequest, CliTableExtensions.GetTable, cancellationToken);
    }

    [NullRequest]
    [Action("remove")]
    [Action("delete")]
    public static async Task<CliActionResponse> RemoveUserById(CliByNameRequest request, CancellationToken cancellationToken = default)
    {
        request ??= new CliByNameRequest();
        var wrapper = await FillGetRequest(request, cancellationToken);
        if (!wrapper.IsSuccessful)
        {
            return new CliActionResponse(wrapper.FailResponse);
        }

        if (!ConfirmAction($"remove resource {request.Name}")) { return CliActionResponse.Empty; }

        var restRequest = new RestRequest("resource/{name}", Method.Delete)
            .AddParameter("name", request.Name ?? string.Empty, ParameterType.UrlSegment);

        return await Execute(restRequest, cancellationToken);
    }

    private static async Task<CliActionResponse> LoadResource(CliLoadResourceFileRequest request, CancellationToken cancellationToken)
    {
        ValidateFileExists(request.File);
        ValidateFileSize(request.File);
        ValidateTextFile(request.File);
        var value = await File.ReadAllTextAsync(request.File, cancellationToken);

        var data = new { request.Name, value };
        var restRequest = new RestRequest(c_resource, Method.Put)
            .AddBody(data);

        var result = await RestProxy.Invoke(restRequest, cancellationToken);
        if (result.StatusCode == HttpStatusCode.NotFound)
        {
            restRequest.Method = Method.Post;
            result = await RestProxy.Invoke(restRequest, cancellationToken);
        }

        return new CliActionResponse(result);
    }

    private static async Task<CliPromptWrapper> FillGetRequest(CliByNameRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Name))
        {
            var p1 = await CliPromptUtil.Resources(cancellationToken);
            if (!p1.IsSuccessful) { return p1; }
            request.Name = p1.Value ?? string.Empty;
        }

        return CliPromptWrapper.Success;
    }
}
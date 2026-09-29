using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Planar.Service.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.API;

public class ResourceDomain(IServiceProvider serviceProvider) : BaseLazyBL<ResourceDomain, IResourceData>(serviceProvider)
{
    private const string kind = "resource";

    public async Task<ApplyResponse> Apply(HttpContext httpContext)
    {
        var yamls = await GetApplyYamls(httpContext, kind);
        var result = await Apply(yamls, httpContext.RequestAborted);
        return result;
    }

    public async Task Add([FromBody] ResourceModel request)
    {
        var resource = Mapper.Map<Resource>(request);
        try
        {
            await DataLayer.AddResource(resource);
        }
        catch (DbUpdateException ex) when (DataLayer.IsDuplicateKey(ex))
        {
            throw new RestConflictException($"resource '{request.Name}' already exists");
        }
    }

    public async Task Update([FromBody] ResourceModel request)
    {
        var resource = Mapper.Map<Resource>(request);
        var count = await DataLayer.UpdateResource(resource);
        if (count == 0)
        {
            throw new RestNotFoundException($"resource '{request.Name}' not found");
        }
    }

    public async Task<PagingResponse<ResourceModel>> GetAll([FromQuery] PagingRequest request)
    {
        var query = DataLayer.GetResources(request);
        var result = await query.ProjectToWithPagingAsync<Resource, ResourceModel>(Mapper, request);

        foreach (var item in result.Data ?? [])
        {
            if (item.Value.Length == 101) { item.Value = string.Concat(item.Value[0..100], "…"); }
        }

        return result;
    }

    public async Task<ResourceModel?> GetByName([FromRoute] string name)
    {
        var item = await DataLayer.GetResource(name)
             ?? throw new RestNotFoundException($"resource '{name}' not found");

        return Mapper.Map<ResourceModel>(item);
    }

    public async Task<IEnumerable<string>> GetAllNames()
    {
        var items = await DataLayer.GetAllResourceNames();
        return items;
    }

    public async Task Delete([FromRoute][Range(1, 100)] string name)
    {
        var count = await DataLayer.RemoveResource(name);
        if (count == 0)
        {
            throw new RestNotFoundException($"resource '{name}' not found");
        }
    }

    internal async Task<ApplyResponse> Apply(IEnumerable<KeyValuePair<string, string>> yamls, CancellationToken cancellationToken)
    {
        // Convert to list of ApplyUserRequest & ApplyPasswordRequest
        var requests = await GetApplyEntities<ApplyResourceRequest>(yamls, kind, cancellationToken, withValidation: true);

        // Validation
        ValidateDuplicateApplyRequests(requests, r => r.Name, kind, "name");

        // Apply changes
        var response = await ApplyChanges(requests);

        return response;
    }

    private async Task<ApplyResponse> ApplyChanges(IReadOnlyCollection<ApplyResourceRequest> requests)
    {
        var response = new ApplyResponse();
        if (requests.Count == 0) { return response; }
        if (requests.Count == 1)
        {
            var request = requests.First();
            var result = await ApplyInner(request);
            response.AddItem(result);
            return response;
        }

        foreach (var request in requests)
        {
            try
            {
                var result = await ApplyInner(request);
                response.AddItem(result);
            }
            catch (Exception ex)
            {
                response.AddItem(new ApplyResponseItem(request.Name ?? string.Empty, ApplyAction.Error, $"fail to handle resource '{request.Name}'. {ex.Message}", Manifest.Resource, request.Source));
            }
        }

        return response;
    }

    private async Task<ApplyResponseItem> ApplyInner(ApplyResourceRequest request)
    {
        request.Name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new RestValidationException("invalid request", "name is required");
        }

        await using var scope = ServiceProvider.CreateAsyncScope();
        var dataLayer = scope.ServiceProvider.GetRequiredService<IResourceData>();
        var exists = await dataLayer.GetResource(request.Name);
        if (exists == null)
        {
            var resource = Mapper.Map<Resource>(request);
            await dataLayer.AddResource(resource);
            return new ApplyResponseItem(request.Name, ApplyAction.Add, $"resource '{request.Name}' was added", Manifest.Resource, request.Source);
        }
        else
        {
            if (exists.Value == request.Value)
            {
                return new ApplyResponseItem(request.Name, ApplyAction.Unchanged, $"resource '{request.Name}' was not changed", Manifest.Resource, request.Source);
            }

            var resource = Mapper.Map<Resource>(request);
            await dataLayer.UpdateResource(resource);
            return new ApplyResponseItem(request.Name, ApplyAction.Update, $"resource '{request.Name}' was updated", Manifest.Resource, request.Source);
        }
    }
}
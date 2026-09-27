using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.Model;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Planar.Service.API;

public class ResourceDomain(IServiceProvider serviceProvider) : BaseLazyBL<ResourceDomain, IResourceData>(serviceProvider)
{
    private const string kind = "resource";

    public async Task<ApplyResponse> Apply(HttpContext httpContext)
    {
        throw new NotImplementedException();
        ////var yamls = await GetApplyYamls(httpContext, kind);
        ////var result = await Apply(yamls, httpContext.RequestAborted);
        ////return result;
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
        var item = await DataLayer.GetResource(name);
        if (item == null) { return null; }

        return Mapper.Map<ResourceModel>(item);
    }

    public async Task Delete([FromRoute][Range(1, 100)] string name)
    {
        var count = await DataLayer.RemoveResource(name);
        if (count == 0)
        {
            throw new RestNotFoundException($"resource '{name}' not found");
        }
    }
}
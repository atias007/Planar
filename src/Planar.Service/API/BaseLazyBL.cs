using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using Planar.Service.Model;
using System;

namespace Planar.Service.API;

public abstract class BaseLazyBL<TBusinessLayer, TDataLayer>(IServiceProvider serviceProvider) : BaseBL<TBusinessLayer>(serviceProvider)
    where TDataLayer : IBaseDataLayer
{
    private readonly Lazy<TDataLayer> _dataLayer = serviceProvider.GetRequiredService<Lazy<TDataLayer>>();

    protected TDataLayer DataLayer => _dataLayer.Value;

    protected static ApplyResponseItem? GetDryRunResponse(HttpContext httpContext, string name, string resource, string source)
    {
        var dryRun = httpContext.Request.Query.ContainsKey("dryRun");
        if (dryRun)
        {
            return new ApplyResponseItem(name, ApplyAction.Skipped, $"resource '{name}' was not applied due to dry run", resource, source);
        }

        return null;
    }
}
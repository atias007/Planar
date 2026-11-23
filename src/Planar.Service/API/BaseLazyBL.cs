using Microsoft.Extensions.DependencyInjection;
using Planar.Service.Data;
using System;

namespace Planar.Service.API;

public abstract class BaseLazyBL<TBusinessLayer, TDataLayer>(IServiceProvider serviceProvider) : BaseBL<TBusinessLayer>(serviceProvider)
    where TDataLayer : IBaseDataLayer
{
    private readonly Lazy<TDataLayer> _dataLayer = serviceProvider.GetRequiredService<Lazy<TDataLayer>>();

    protected TDataLayer DataLayer => _dataLayer.Value;
}
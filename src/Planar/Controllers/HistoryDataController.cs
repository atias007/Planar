﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Planar.Authorization;
using Planar.Service.API;
using System;

namespace Planar.Controllers;

[ViewerAuthorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class HistoryDataController(IServiceProvider serviceProvider) : ControllerBase
{
    private readonly HistoryDomain _businessLayer = serviceProvider.GetRequiredService<HistoryDomain>();

    [EnableQuery]
    public IActionResult Get()
    {
        var result = _businessLayer.GetHistoryData();
        return Ok(result);
    }

    [EnableQuery]
    public IActionResult Get([FromODataUri] long key)
    {
        var result = _businessLayer.GetHistory(key);
        return Ok(result);
    }
}
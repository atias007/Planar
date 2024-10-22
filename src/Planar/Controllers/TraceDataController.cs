using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Planar.Authorization;
using Planar.Service.API;
using System;

namespace Planar.Controllers;

[ViewerAuthorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class TraceDataController(IServiceProvider serviceProvider) : ControllerBase
{
    private readonly TraceDomain _businessLayer = serviceProvider.GetRequiredService<TraceDomain>();

    [EnableQuery]
    public IActionResult Get()
    {
        var result = _businessLayer.GetTraceData();
        return Ok(result);
    }

    [EnableQuery]
    public IActionResult Get([FromODataUri] int key)
    {
        var result = _businessLayer.GetTrace(key);
        return Ok(result);
    }
}
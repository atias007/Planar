using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using Planar.Validation.Attributes;
using System.Threading.Tasks;

namespace Planar.Controllers;

[ApiController]
[Route("trace")]
[ViewerAuthorize]
public class TraceController(TraceDomain bl) : BaseController<TraceDomain>(bl)
{
    [HttpGet]
    [EndpointName("get_trace")]
    [EndpointDescription("Get trace records")]
    [EndpointSummary("Get Trace Records")]
    [OkJsonResponse(typeof(PagingResponse<LogDetails>))]
    public async Task<ActionResult<PagingResponse<LogDetails>>> Get([FromQuery] GetTraceRequest request)
    {
        var result = await BusinesLayer.Get(request);
        return Ok(result);
    }

    [HttpGet("{id}/exception")]
    [EndpointName("get_trace_id_exception")]
    [EndpointDescription("Get trace excption for record")]
    [EndpointSummary("Get Trace Exception")]
    [OkJsonResponse(typeof(string))]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<ActionResult<string>> GetException([FromRoute][Id] int id)
    {
        var result = await BusinesLayer.GetException(id);
        return Ok(result);
    }

    [HttpGet("{id}/properties")]
    [EndpointName("get_trace_id_properties")]
    [EndpointDescription("Get trace properties for record")]
    [EndpointSummary("Get Trace Properties")]
    [OkJsonResponse(typeof(string))]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<ActionResult<string>> GetProperties([FromRoute][Id] int id)
    {
        var result = await BusinesLayer.GetProperties(id);
        return Ok(result);
    }

    [HttpGet("count")]
    [EndpointName("get_trace_count")]
    [EndpointDescription("Get trace count by level")]
    [EndpointSummary("Get Trace Count")]
    [OkJsonResponse(typeof(CounterResponse))]
    [BadRequestResponse]
    public async Task<ActionResult<CounterResponse>> GetTraceCounter([FromQuery] CounterRequest request)
    {
        var result = await BusinesLayer.GetTraceCounter(request);
        return Ok(result);
    }
}
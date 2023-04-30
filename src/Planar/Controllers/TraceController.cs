using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using Planar.Validation.Attributes;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [Route("trace")]
    [ViewerAuthorize]
    public class TraceController : BaseController<TraceDomain>
    {
        public TraceController(TraceDomain bl) : base(bl)
        {
        }

        [HttpGet]
        [SwaggerOperation(OperationId = "get_trace", Description = "Get trace records", Summary = "Get Trace Records")]
        [OkJsonResponse(typeof(List<LogDetails>))]
        public async Task<ActionResult<List<LogDetails>>> Get([FromQuery] GetTraceRequest request)
        {
            var result = await BusinesLayer.Get(request);
            return Ok(result);
        }

        [HttpGet("{id}/exception")]
        [SwaggerOperation(OperationId = "get_trace_id_exception", Description = "Get trace excption for record", Summary = "Get Trace Exception")]
        [OkJsonResponse(typeof(string))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<string>> GetException([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetException(id);
            return Ok(result);
        }

        [HttpGet("{id}/properties")]
        [SwaggerOperation(OperationId = "get_trace_id_properties", Description = "Get trace properties for record", Summary = "Get Trace Properties")]
        [OkJsonResponse(typeof(string))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<string>> GetProperties([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetProperties(id);
            return Ok(result);
        }

        [HttpGet("count")]
        [SwaggerOperation(OperationId = "get_trace_count", Description = "Get trace count by level", Summary = "Get Trace Count")]
        [OkJsonResponse(typeof(CounterResponse))]
        [BadRequestResponse]
        public async Task<ActionResult<CounterResponse>> GetTraceCounter([FromQuery] CounterRequest request)
        {
            var result = await BusinesLayer.GetTraceCounter(request);
            return Ok(result);
        }
    }
}
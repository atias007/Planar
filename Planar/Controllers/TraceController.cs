using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.API;
using Planar.Service.API.Validation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [ApiController]
    [Route("trace")]
    public class TraceController : BaseController<TraceController, TraceDomain>
    {
        public TraceController(ILogger<TraceController> logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        [HttpGet]
        public async Task<ActionResult<List<LogDetails>>> Get([FromQuery] GetTraceRequest request)
        {
            var result = await BusinesLayer.Get(request);
            return Ok(result);
        }

        [HttpGet("{id}/exception")]
        public async Task<ActionResult<string>> GetException([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetException(id);
            return Ok(result);
        }

        [HttpGet("{id}/properties")]
        public async Task<ActionResult<string>> GetProperties([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetProperties(id);
            return Ok(result);
        }
    }
}
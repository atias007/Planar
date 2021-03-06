using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.API;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [ApiController]
    [Route("parameters")]
    public class ParametersController : BaseController<ParametersController, ParametersDomain>
    {
        public ParametersController(ILogger<ParametersController> logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        [HttpGet]
        public async Task<ActionResult<Dictionary<string, string>>> GetAll()
        {
            var result = await BusinesLayer.GetAll();
            return Ok(result);
        }

        [HttpGet("{key}")]
        public async Task<ActionResult<string>> Get([FromRoute] string key)
        {
            var result = await BusinesLayer.Get(key);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult> Upsert([FromBody] GlobalParameterData request)
        {
            await BusinesLayer.Upsert(request);
            return CreatedAtAction(nameof(Get), new { key = request.Key }, null);
        }

        [HttpDelete("{key}")]
        public async Task<ActionResult> Delete([FromRoute] string key)
        {
            await BusinesLayer.Delete(key);
            return NoContent();
        }

        [HttpPost("flush")]
        public async Task<ActionResult> Flush()
        {
            await BusinesLayer.Flush();
            return NoContent();
        }
    }
}
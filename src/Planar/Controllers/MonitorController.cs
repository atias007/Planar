using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.API;
using Planar.Validation.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [ApiController]
    [Route("monitor")]
    public class MonitorController : BaseController<MonitorController, MonitorDomain>
    {
        public MonitorController(ILogger<MonitorController> logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        [HttpGet]
        public async Task<ActionResult<List<MonitorItem>>> Get()
        {
            var result = await BusinesLayer.Get(null);
            return Ok(result);
        }

        [HttpGet("{jobOrGroupId}")]
        public async Task<ActionResult<List<MonitorItem>>> Get([FromRoute][Required] string jobOrGroupId)
        {
            var result = await BusinesLayer.Get(jobOrGroupId);
            return Ok(result);
        }

        [HttpGet("metadata")]
        public async Task<ActionResult<MonitorActionMedatada>> GetMetadata()
        {
            var result = await BusinesLayer.GetMedatada();
            return Ok(result);
        }

        [HttpGet("events")]
        public ActionResult<List<string>> GetEvents()
        {
            var result = BusinesLayer.GetEvents();
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<int>> Add([FromBody] AddMonitorRequest request)
        {
            var result = await BusinesLayer.Add(request);
            return CreatedAtAction(nameof(Get), result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete([FromRoute][Id] int id)
        {
            await BusinesLayer.Delete(id);
            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> Update([FromRoute][Id] int id, [FromBody] UpdateEntityRecord request)
        {
            await BusinesLayer.Update(id, request);
            return NoContent();
        }

        [HttpPost("reload")]
        public async Task<ActionResult<string>> Reload()
        {
            var result = await BusinesLayer.Reload();
            return Ok(result);
        }

        [HttpGet("hooks")]
        public ActionResult<List<string>> GetHooks()
        {
            var result = BusinesLayer.GetHooks();
            return Ok(result);
        }
    }
}
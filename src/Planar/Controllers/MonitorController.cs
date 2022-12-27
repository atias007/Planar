using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Service.API;
using Planar.Validation.Attributes;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [Route("monitor")]
    public class MonitorController : BaseController<MonitorDomain>
    {
        public MonitorController(MonitorDomain bl) : base(bl)
        {
        }

        [HttpGet]
        public async Task<ActionResult<List<MonitorItem>>> GetAll()
        {
            var result = await BusinesLayer.GetAll();
            return Ok(result);
        }

        [HttpGet("byKey/{key}")]
        public async Task<ActionResult<List<MonitorItem>>> GetByKey([FromRoute][Required] string key)
        {
            var result = await BusinesLayer.GetByKey(key);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MonitorItem>> GetById([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetById(id);
            return Ok(result);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
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
        [JsonConsumes]
        public async Task<ActionResult<EntityIdResponse>> Add([FromBody] AddMonitorRequest request)
        {
            var result = await BusinesLayer.Add(request);
            return CreatedAtAction(nameof(GetById), new { id = result }, new EntityIdResponse(result));
        }

        [HttpPut]
        [JsonConsumes]
        [SwaggerOperation(OperationId = "put_monitor", Description = "Update monitor", Summary = "Update Monitor")]
        [NoContentResponse]
        [BadRequestResponse]
        [ConflictResponse]
        [NotFoundResponse]
        public async Task<IActionResult> Update([FromBody] UpdateMonitorRequest request)
        {
            await BusinesLayer.Update(request);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete([FromRoute][Id] int id)
        {
            await BusinesLayer.Delete(id);
            return NoContent();
        }

        [HttpPatch]
        [JsonConsumes]
        public async Task<ActionResult> UpdatePartial([FromBody] UpdateEntityRecord request)
        {
            await BusinesLayer.UpdatePartial(request);
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
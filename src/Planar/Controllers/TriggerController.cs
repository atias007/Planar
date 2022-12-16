using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Service.API;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [Route("trigger")]
    public class TriggerController : BaseController<TriggerDomain>
    {
        public TriggerController(TriggerDomain bl) : base(bl)
        {
        }

        [HttpGet("{triggerId}")]
        public async Task<ActionResult<TriggerRowDetails>> Get([FromRoute] string triggerId)
        {
            var result = await BusinesLayer.Get(triggerId);
            return Ok(result);
        }

        [HttpGet("{jobId}/byjob")]
        public async Task<ActionResult<TriggerRowDetails>> GetByJob([FromRoute] string jobId)
        {
            var result = await BusinesLayer.GetByJob(jobId);
            return Ok(result);
        }

        [HttpDelete("{triggerId}")]
        public async Task<ActionResult> Delete([FromRoute] string triggerId)
        {
            await BusinesLayer.Delete(triggerId);
            return NoContent();
        }

        [HttpPost("pause")]
        public async Task<ActionResult> Pause([FromBody] JobOrTriggerKey request)
        {
            await BusinesLayer.Pause(request);
            return NoContent();
        }

        [HttpPost("resume")]
        public async Task<ActionResult> Resume([FromBody] JobOrTriggerKey request)
        {
            await BusinesLayer.Resume(request);
            return NoContent();
        }

        [HttpPost("data")]
        public async Task<IActionResult> AddData([FromBody] JobOrTriggerDataRequest request)
        {
            await BusinesLayer.UpsertData(request, JobDomain.UpsertMode.Add);
            return CreatedAtAction(nameof(Get), new { triggerId = request.Id }, null);
        }

        [HttpPut("data")]
        public async Task<IActionResult> UpdateData([FromBody] JobOrTriggerDataRequest request)
        {
            await BusinesLayer.UpsertData(request, JobDomain.UpsertMode.Update);
            return CreatedAtAction(nameof(Get), new { triggerId = request.Id }, null);
        }

        [HttpDelete("{id}/data/{key}")]
        public async Task<IActionResult> RemoveData([FromRoute][Required] string id, [FromRoute][Required] string key)
        {
            await BusinesLayer.RemoveData(id, key);
            return NoContent();
        }
    }
}
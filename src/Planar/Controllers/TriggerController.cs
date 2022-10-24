using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Service.API;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [ApiController]
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

        [HttpPost]
        public async Task<ActionResult> Add([FromBody] AddTriggerRequest request)
        {
            var jobId = await BusinesLayer.Add(request);
            return CreatedAtAction(nameof(GetByJob), new { jobId }, new { Id = jobId });
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
    }
}
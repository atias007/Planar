using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [ApiController]
    [Route("trigger")]
    public class TriggerController(TriggerDomain bl) : BaseController<TriggerDomain>(bl)
    {
        [HttpGet("{triggerId}")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_trigger_triggerid", Description = "Get trigger by id", Summary = "Get Trigger")]
        [BadRequestResponse]
        [OkJsonResponse(typeof(TriggerRowDetails))]
        public async Task<ActionResult<TriggerRowDetails>> Get([FromRoute][Required] string triggerId)
        {
            var result = await BusinesLayer.Get(triggerId);
            return Ok(result);
        }

        [HttpGet("{jobId}/by-job")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_trigger_jobid_by_job", Description = "Find triggers by job ", Summary = "Find Triggers By Job")]
        [NotFoundResponse]
        [BadRequestResponse]
        [OkJsonResponse(typeof(TriggerRowDetails))]
        public async Task<ActionResult<TriggerRowDetails>> GetByJob([FromRoute][Required] string jobId)
        {
            var result = await BusinesLayer.GetByJob(jobId);
            return Ok(result);
        }

        [HttpDelete("{triggerId}")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "delete_trigger_triggerId", Description = "Delete trigger", Summary = "Delete Trigger")]
        [NotFoundResponse]
        [BadRequestResponse]
        [NoContentResponse]
        public async Task<ActionResult> Delete([FromRoute][Required] string triggerId)
        {
            await BusinesLayer.Delete(triggerId);
            return NoContent();
        }

        [HttpPatch("cron-expression")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "update_trigger_cron_expression", Description = "Update trigger cron expression", Summary = "Update Trigger Cron Expression")]
        [NotFoundResponse]
        [BadRequestResponse]
        [NoContentResponse]
        public async Task<ActionResult> UpdateCron([FromBody] UpdateCronRequest request)
        {
            await BusinesLayer.UpdateCron(request);
            return NoContent();
        }

        [HttpPatch("interval")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "update_trigger_interval", Description = "Update trigger interval", Summary = "Update Trigger Cron Interval")]
        [NotFoundResponse]
        [BadRequestResponse]
        [NoContentResponse]
        public async Task<ActionResult> UpdateInterval([FromBody] UpdateIntervalRequest request)
        {
            await BusinesLayer.UpdateInterval(request);
            return NoContent();
        }

        [HttpPost("pause")]
        [EditorAuthorize]
        [JsonConsumes]
        [SwaggerOperation(OperationId = "post_trigger_pause", Description = "Pause trigger", Summary = "Pause Trigger")]
        [NotFoundResponse]
        [BadRequestResponse]
        [NoContentResponse]
        public async Task<ActionResult> Pause([FromBody] JobOrTriggerKey request)
        {
            await BusinesLayer.Pause(request);
            return NoContent();
        }

        [HttpPost("resume")]
        [EditorAuthorize]
        [JsonConsumes]
        [SwaggerOperation(OperationId = "post_trigger_resume", Description = "Resume trigger", Summary = "Resume Trigger")]
        [NotFoundResponse]
        [BadRequestResponse]
        [NoContentResponse]
        public async Task<ActionResult> Resume([FromBody] JobOrTriggerKey request)
        {
            await BusinesLayer.Resume(request);
            return NoContent();
        }

        [HttpPost("data")]
        [EditorAuthorize]
        [JsonConsumes]
        [SwaggerOperation(OperationId = "post_trigger_data", Description = "Add trigger data", Summary = "Add Trigger Data")]
        [CreatedResponse]
        [NotFoundResponse]
        [BadRequestResponse]
        public async Task<IActionResult> AddData([FromBody] JobOrTriggerDataRequest request)
        {
            await BusinesLayer.PutData(request, JobDomain.PutMode.Add);
            return CreatedAtAction(nameof(Get), new { triggerId = request.Id }, null);
        }

        [HttpPut("data")]
        [EditorAuthorize]
        [JsonConsumes]
        [SwaggerOperation(OperationId = "put_trigger_data", Description = "Update trigger data", Summary = "Update Trigger Data")]
        [CreatedResponse]
        [NotFoundResponse]
        [BadRequestResponse]
        public async Task<IActionResult> UpdateData([FromBody] JobOrTriggerDataRequest request)
        {
            await BusinesLayer.PutData(request, JobDomain.PutMode.Update);
            return CreatedAtAction(nameof(Get), new { triggerId = request.Id }, null);
        }

        [HttpDelete("{id}/data/{key}")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "delete_trigger_id_data_key", Description = "Delete trigger data", Summary = "Delete Trigger Data")]
        [NoContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<IActionResult> RemoveData([FromRoute][Required] string id, [FromRoute][Required] string key)
        {
            await BusinesLayer.RemoveData(id, key);
            return NoContent();
        }

        [HttpGet("cron")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_trigger_cron_expression", Description = "Get description of cron expression", Summary = "Get Cron Description")]
        [OkTextResponse]
        [BadRequestResponse]
        public ActionResult<string> GetCronDescription([FromQuery][Required] string expression)
        {
            var result = BusinesLayer.GetCronDescription(expression);
            return Ok(result);
        }

        [HttpGet("paused")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_trigger_paused", Description = "Get all paused triggers", Summary = "Get Paused Triggers")]
        [OkJsonResponse(typeof(IEnumerable<PausedTriggerDetails>))]
        public async Task<ActionResult<IEnumerable<PausedTriggerDetails>>> GetPausedTriggers()
        {
            var result = await BusinesLayer.GetPausedTriggers();
            return Ok(result);
        }
    }
}
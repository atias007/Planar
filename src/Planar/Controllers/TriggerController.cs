using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;

namespace Planar.Controllers;

[ApiController]
[Route("trigger")]
public class TriggerController(TriggerDomain bl) : BaseController<TriggerDomain>(bl)
{
    [HttpGet("{triggerId}")]
    [ViewerAuthorize]
    [EndpointName("get_trigger_triggerid")]
    [EndpointDescription("Get trigger by id")]
    [EndpointSummary("Get Trigger")]
    [BadRequestResponse]
    [OkJsonResponse(typeof(TriggerRowDetails))]
    public async Task<ActionResult<TriggerRowDetails>> Get([FromRoute][Required] string triggerId)
    {
        var result = await BusinesLayer.Get(triggerId);
        return Ok(result);
    }

    [HttpGet("{jobId}/by-job")]
    [ViewerAuthorize]
    [EndpointName("get_trigger_jobid_by_job")]
    [EndpointDescription("Find triggers by job ")]
    [EndpointSummary("Find Triggers By Job")]
    [NotFoundResponse]
    [BadRequestResponse]
    [OkJsonResponse(typeof(TriggerRowDetails))]
    public async Task<ActionResult<TriggerRowDetails>> GetByJob([FromRoute][Required] string jobId)
    {
        var result = await BusinesLayer.GetByJob(jobId);
        return Ok(result);
    }

    [HttpGet("ids")]
    [AllowAnonymous]
    [EndpointName("get_trigger_ids")]
    [EndpointDescription("Get all trigger ids")]
    [EndpointSummary("Get All Trigger Ids")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [OkJsonResponse(typeof(IEnumerable<string>))]
    public async Task<ActionResult<IEnumerable<string>>> GetAllIds()
    {
        var result = await BusinesLayer.GetAllIds();
        return Ok(result);
    }

    [HttpDelete("{triggerId}")]
    [EditorAuthorize]
    [EndpointName("delete_trigger_triggerId")]
    [EndpointDescription("Delete trigger")]
    [EndpointSummary("Delete Trigger")]
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
    [JsonConsumes]
    [EndpointName("update_trigger_cron_expression")]
    [EndpointDescription("Update cron trigger expression")]
    [EndpointSummary("Update Cron Trigger Expression")]
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
    [JsonConsumes]
    [EndpointName("update_trigger_interval")]
    [EndpointDescription("Update simple trigger interval")]
    [EndpointSummary("Update Simple Trigger Interval")]
    [NotFoundResponse]
    [BadRequestResponse]
    [NoContentResponse]
    public async Task<ActionResult> UpdateInterval([FromBody] UpdateIntervalRequest request)
    {
        await BusinesLayer.UpdateInterval(request);
        return NoContent();
    }

    [HttpPatch("timeout")]
    [EditorAuthorize]
    [JsonConsumes]
    [EndpointName("update_trigger_timeout")]
    [EndpointDescription("Update trigger timeout")]
    [EndpointSummary("Update Trigger Timeout")]
    [NotFoundResponse]
    [BadRequestResponse]
    [NoContentResponse]
    public async Task<ActionResult> UpdateTimeout([FromBody] UpdateTimeoutRequest request)
    {
        await BusinesLayer.UpdateTimeout(request);
        return NoContent();
    }

    [HttpPost("pause")]
    [EditorAuthorize]
    [JsonConsumes]
    [EndpointName("post_trigger_pause")]
    [EndpointDescription("Pause trigger")]
    [EndpointSummary("Pause Trigger")]
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
    [EndpointName("post_trigger_resume")]
    [EndpointDescription("Resume trigger")]
    [EndpointSummary("Resume Trigger")]
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
    [EndpointName("post_trigger_data")]
    [EndpointDescription("Add trigger data")]
    [EndpointSummary("Add Trigger Data")]
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
    [EndpointName("put_trigger_data")]
    [EndpointDescription("Update trigger data")]
    [EndpointSummary("Update Trigger Data")]
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
    [EndpointName("delete_trigger_id_data_key")]
    [EndpointDescription("Delete trigger data")]
    [EndpointSummary("Delete Trigger Data")]
    [NoContentResponse]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<IActionResult> RemoveData([FromRoute][Required] string id, [FromRoute][Required] string key)
    {
        key = WebUtility.UrlDecode(key);
        await BusinesLayer.RemoveData(id, key);
        return NoContent();
    }

    [HttpDelete("{id}/data")]
    [EditorAuthorize]
    [EndpointName("delete_trigger_id_data")]
    [EndpointDescription("Delete all trigger data")]
    [EndpointSummary("Delete All Trigger Data")]
    [NoContentResponse]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<IActionResult> ClearData([FromRoute][Required] string id)
    {
        await BusinesLayer.ClearData(id);
        return NoContent();
    }

    [HttpGet("cron")]
    [ViewerAuthorize]
    [EndpointName("get_trigger_cron_expression")]
    [EndpointDescription("Get description of cron expression")]
    [EndpointSummary("Get Cron Description")]
    [OkTextResponse]
    [BadRequestResponse]
    public ActionResult<string> GetCronDescription([FromQuery][Required] string expression)
    {
        var result = TriggerDomain.GetCronDescription(expression);
        return Ok(result);
    }

    [HttpGet("paused")]
    [ViewerAuthorize]
    [EndpointName("get_trigger_paused")]
    [EndpointDescription("Get all paused triggers")]
    [EndpointSummary("Get Paused Triggers")]
    [OkJsonResponse(typeof(IEnumerable<PausedTriggerDetails>))]
    public async Task<ActionResult<IEnumerable<PausedTriggerDetails>>> GetPausedTriggers()
    {
        var result = await BusinesLayer.GetPausedTriggers();
        return Ok(result);
    }
}
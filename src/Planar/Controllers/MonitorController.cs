using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using Planar.Validation.Attributes;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [ApiController]
    [Route("monitor")]
    public class MonitorController : BaseController<MonitorDomain>
    {
        public MonitorController(MonitorDomain bl) : base(bl)
        {
        }

        [HttpGet]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_monitor", Description = "Get all monitors", Summary = "Get All Monitors")]
        [OkJsonResponse(typeof(PagingResponse<MonitorItem>))]
        public async Task<ActionResult<PagingResponse<MonitorItem>>> GetAll([FromQuery] PagingRequest request)
        {
            var result = await BusinesLayer.GetAll(request);
            return Ok(result);
        }

        [HttpGet("by-Job/{jobId}")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_monitor_by_job_id", Description = "Get monitor by job key or id", Summary = "Get Monitor By Job")]
        [OkJsonResponse(typeof(List<MonitorItem>))]
        [BadRequestResponse]
        public async Task<ActionResult<List<MonitorItem>>> GetByJob([FromRoute][Required] string jobId)
        {
            var result = await BusinesLayer.GetByJob(jobId);
            return Ok(result);
        }

        [HttpGet("by-group/{group}")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_monitor_by_group_group", Description = "Get monitor by job group", Summary = "Get Monitor By Group")]
        [OkJsonResponse(typeof(List<MonitorItem>))]
        [BadRequestResponse]
        public async Task<ActionResult<List<MonitorItem>>> GetByGroup([FromRoute][Required] string group)
        {
            var result = await BusinesLayer.GetByGroup(group);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_monitor_id", Description = "Get monitor by id", Summary = "Get Monitor")]
        [OkJsonResponse(typeof(MonitorItem))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<MonitorItem>> GetById([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetById(id);
            return Ok(result);
        }

        [HttpGet("alert/{id}")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_monitor_alert_id", Description = "Get monitor alert by id", Summary = "Get Monitor Alert")]
        [OkJsonResponse(typeof(MonitorAlertModel))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<MonitorAlertModel>> GetAlertById([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetMonitorAlert(id);
            return Ok(result);
        }

        [HttpGet("alerts")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_monitor_alerts", Description = "Get monitor alerts by filter", Summary = "Get Monitor Alerts")]
        [OkJsonResponse(typeof(PagingResponse<MonitorAlertRowModel>))]
        [BadRequestResponse]
        public async Task<ActionResult<PagingResponse<MonitorAlertRowModel>>> GetMonitorAlerts([FromQuery] GetMonitorsAlertsRequest request)
        {
            var result = await BusinesLayer.GetMonitorsAlerts(request);
            return Ok(result);
        }

        [HttpGet("events")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_monitor_events", Description = "Get all monitor events type", Summary = "Get All Monitor Events")]
        [OkJsonResponse(typeof(List<MonitorEventModel>))]
        public ActionResult<List<MonitorEventModel>> GetEvents()
        {
            var result = MonitorDomain.GetEvents();
            return Ok(result);
        }

        [HttpPost]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "post_monitor", Description = "Add monitor", Summary = "Add Monitor")]
        [JsonConsumes]
        [CreatedResponse(typeof(EntityIdResponse))]
        [BadRequestResponse]
        [ConflictResponse]
        public async Task<ActionResult<EntityIdResponse>> Add([FromBody] AddMonitorRequest request)
        {
            var result = await BusinesLayer.Add(request);
            return CreatedAtAction(nameof(GetById), new { id = result }, new EntityIdResponse(result));
        }

        [HttpPut]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "put_monitor", Description = "Update monitor", Summary = "Update Monitor")]
        [JsonConsumes]
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
        [AdministratorAuthorize]
        [SwaggerOperation(OperationId = "delete_monitor_id", Description = "Delete monitor", Summary = "Delete Monitor")]
        [BadRequestResponse]
        [NoContentResponse]
        [NotFoundResponse]
        public async Task<ActionResult> Delete([FromRoute][Id] int id)
        {
            await BusinesLayer.Delete(id);
            return NoContent();
        }

        [HttpPatch]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "patch_monitor", Description = "Update monitor single property", Summary = "Partial Update Monitor")]
        [JsonConsumes]
        [BadRequestResponse]
        [NoContentResponse]
        [BadRequestResponse]
        [ConflictResponse]
        [NotFoundResponse]
        public async Task<ActionResult> UpdatePartial([FromBody] UpdateEntityRequestById request)
        {
            await BusinesLayer.PartialUpdateMonitor(request);
            return NoContent();
        }

        [HttpPost("reload")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "post_monitor_reload", Description = "Refresh monitor hooks", Summary = "Refresh Monitor Hooks")]
        [OkJsonResponse(typeof(string))]
        public async Task<ActionResult<string>> Reload()
        {
            var result = await BusinesLayer.Reload();
            return Ok(result);
        }

        [HttpGet("hooks")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_monitor_hooks", Description = "Get all monitor hooks", Summary = "Get Monitor Hooks")]
        [OkJsonResponse(typeof(string))]
        public ActionResult<List<string>> GetHooks()
        {
            var result = BusinesLayer.GetHooks();
            return Ok(result);
        }

        [HttpPost("try")]
        [EditorAuthorize]
        [JsonConsumes]
        [SwaggerOperation(OperationId = "post_monitor_try", Description = "Try monitor", Summary = "Try Monitor")]
        [NoContentResponse]
        [BadRequestResponse]
        public async Task<ActionResult> Try(MonitorTestRequest request)
        {
            await BusinesLayer.Try(request);
            return NoContent();
        }

        [HttpPatch("mute")]
        [EditorAuthorize]
        [JsonConsumes]
        [SwaggerOperation(OperationId = "patch_monitor_mute", Description = "Mute monitor", Summary = "Mute Monitor")]
        [NoContentResponse]
        [BadRequestResponse]
        public async Task<IActionResult> Mute(MonitorMuteRequest request)
        {
            await BusinesLayer.Mute(request);
            return NoContent();
        }

        [HttpPatch("unmute")]
        [EditorAuthorize]
        [JsonConsumes]
        [SwaggerOperation(OperationId = "patch_monitor_unmute", Description = "Unmute monitor", Summary = "Unmute Monitor")]
        [NoContentResponse]
        [BadRequestResponse]
        public async Task<IActionResult> Unmute(MonitorUnmuteRequest request)
        {
            await BusinesLayer.UnMute(request);
            return NoContent();
        }
    }
}
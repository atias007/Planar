using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using Planar.Validation.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;

namespace Planar.Controllers;

[ApiController]
[Route("monitor")]
public class MonitorController(MonitorDomain bl) : BaseController<MonitorDomain>(bl)
{
    [HttpGet]
    [EditorAuthorize]
    [EndpointName("get_monitor")]
    [EndpointDescription("Get all monitors")]
    [EndpointSummary("Get All Monitors")]
    [OkJsonResponse(typeof(PagingResponse<MonitorItem>))]
    public async Task<ActionResult<PagingResponse<MonitorItem>>> GetAll([FromQuery] PagingRequest request)
    {
        var result = await BusinesLayer.GetAll(request);
        return Ok(result);
    }

    [HttpGet("by-Job/{jobId}")]
    [EditorAuthorize]
    [EndpointName("get_monitor_by_job_id")]
    [EndpointDescription("Get monitor by job key or id")]
    [EndpointSummary("Get Monitor By Job")]
    [OkJsonResponse(typeof(List<MonitorItem>))]
    [BadRequestResponse]
    public async Task<ActionResult<List<MonitorItem>>> GetByJob([FromRoute][Required] string jobId)
    {
        var result = await BusinesLayer.GetByJob(jobId);
        return Ok(result);
    }

    [HttpGet("by-group/{group}")]
    [EditorAuthorize]
    [EndpointName("get_monitor_by_group_group")]
    [EndpointDescription("Get monitor by job group")]
    [EndpointSummary("Get Monitor By Group")]
    [OkJsonResponse(typeof(List<MonitorItem>))]
    [BadRequestResponse]
    public async Task<ActionResult<List<MonitorItem>>> GetByGroup([FromRoute][Required] string group)
    {
        group = WebUtility.UrlDecode(group);
        var result = await BusinesLayer.GetMonitorActionsByGroup(group);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [EditorAuthorize]
    [EndpointName("get_monitor_id")]
    [EndpointDescription("Get monitor by id")]
    [EndpointSummary("Get Monitor")]
    [OkJsonResponse(typeof(MonitorItem))]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<ActionResult<MonitorItem>> GetById([FromRoute][Id] int id)
    {
        var result = await BusinesLayer.GetMonitorActionById(id);
        return Ok(result);
    }

    [HttpGet("alert/{id}")]
    [ViewerAuthorize]
    [EndpointName("get_monitor_alert_id")]
    [EndpointDescription("Get monitor alert by id")]
    [EndpointSummary("Get Monitor Alert")]
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
    [EndpointName("get_monitor_alerts")]
    [EndpointDescription("Get monitor alerts by filter")]
    [EndpointSummary("Get Monitor Alerts")]
    [OkJsonResponse(typeof(PagingResponse<MonitorAlertRowModel>))]
    [BadRequestResponse]
    public async Task<ActionResult<PagingResponse<MonitorAlertRowModel>>> GetMonitorAlerts([FromQuery] GetMonitorsAlertsRequest request)
    {
        var result = await BusinesLayer.GetMonitorsAlerts(request);
        return Ok(result);
    }

    [HttpGet("events")]
    [EditorAuthorize]
    [EndpointName("get_monitor_events")]
    [EndpointDescription("Get all monitor events type")]
    [EndpointSummary("Get All Monitor Events")]
    [OkJsonResponse(typeof(List<MonitorEventModel>))]
    public ActionResult<List<MonitorEventModel>> GetEvents()
    {
        var result = MonitorDomain.GetEvents();
        return Ok(result);
    }

    [HttpPost]
    [EditorAuthorize]
    [EndpointName("post_monitor")]
    [EndpointDescription("Add monitor")]
    [EndpointSummary("Add Monitor")]
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
    [EndpointName("put_monitor")]
    [EndpointDescription("Update monitor")]
    [EndpointSummary("Update Monitor")]
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
    [EndpointName("delete_monitor_id")]
    [EndpointDescription("Delete monitor")]
    [EndpointSummary("Delete Monitor")]
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
    [EndpointName("patch_monitor")]
    [EndpointDescription("Update monitor single property")]
    [EndpointSummary("Partial Update Monitor")]
    [JsonConsumes]
    [BadRequestResponse]
    [NoContentResponse]
    [ConflictResponse]
    [NotFoundResponse]
    public async Task<ActionResult> UpdatePartial([FromBody] UpdateEntityRequestById request)
    {
        await BusinesLayer.PartialUpdateMonitor(request);
        return NoContent();
    }

    [HttpPatch("add-distribution-group")]
    [EditorAuthorize]
    [EndpointName("patch_monitor_add_distribution_group")]
    [EndpointDescription("Add distribution group to monitor")]
    [EndpointSummary("Add Distribution Group To Monitor")]
    [JsonConsumes]
    [BadRequestResponse]
    [NoContentResponse]
    [ConflictResponse]
    [NotFoundResponse]
    public async Task<ActionResult> AddDistributionGroup([FromBody] MonitorGroupRequest request)
    {
        await BusinesLayer.AddDistributionGroup(request);
        return NoContent();
    }

    [HttpPatch("remove-distribution-group")]
    [EditorAuthorize]
    [EndpointName("patch_monitor_remove_distribution_group")]
    [EndpointDescription("Remove distribution group from monitor")]
    [EndpointSummary("Remove Distribution Group From Monitor")]
    [JsonConsumes]
    [BadRequestResponse]
    [NoContentResponse]
    [ConflictResponse]
    [NotFoundResponse]
    public async Task<ActionResult> RemoveDistributionGroup([FromBody] MonitorGroupRequest request)
    {
        await BusinesLayer.RemoveDistributionGroup(request);
        return NoContent();
    }

    [HttpGet("hooks")]
    [EditorAuthorize]
    [EndpointName("get_monitor_hooks")]
    [EndpointDescription("Get all monitor hooks")]
    [EndpointSummary("Get Monitor Hooks")]
    [OkJsonResponse(typeof(IEnumerable<HookInfo>))]
    public ActionResult<IEnumerable<HookInfo>> GetHooks()
    {
        var result = BusinesLayer.GetHooks();
        return Ok(result);
    }

    [HttpGet("new-hooks")]
    [EditorAuthorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    [OkJsonResponse(typeof(IEnumerable<string>))]
    public async Task<ActionResult<IEnumerable<string>>> NewHooks()
    {
        var result = await BusinesLayer.SearchNewHooks();
        return Ok(result);
    }

    [HttpDelete("hook/{name}")]
    [AdministratorAuthorize]
    [EndpointName("delete_monitor_hook_name")]
    [EndpointDescription("Delete monitor hook")]
    [EndpointSummary("Delete Monitor Hook")]
    [BadRequestResponse]
    [NoContentResponse]
    [NotFoundResponse]
    public async Task<ActionResult> DeleteHook([FromRoute][Required] string name)
    {
        name = WebUtility.UrlDecode(name);
        await BusinesLayer.DeleteHook(name);
        return NoContent();
    }

    [HttpPost("hook")]
    [AdministratorAuthorize]
    [EndpointName("add_monitor_hook")]
    [EndpointDescription("Add monitor hook")]
    [EndpointSummary("Add Monitor Hook")]
    [JsonConsumes]
    [BadRequestResponse]
    [CreatedResponse(typeof(MonitorHookDetails))]
    public async Task<ActionResult<MonitorHookDetails>> AddHook([FromBody][Required] AddHookRequest request)
    {
        var result = await BusinesLayer.AddHook(request);
        return CreatedAtAction(null, result);
    }

    [HttpPost("try")]
    [EditorAuthorize]
    [JsonConsumes]
    [EndpointName("post_monitor_try")]
    [EndpointDescription("Try monitor")]
    [EndpointSummary("Try Monitor")]
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
    [EndpointName("patch_monitor_mute")]
    [EndpointDescription("Mute monitor")]
    [EndpointSummary("Mute Monitor")]
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
    [EndpointName("patch_monitor_unmute")]
    [EndpointDescription("Unmute monitor")]
    [EndpointSummary("Unmute Monitor")]
    [NoContentResponse]
    [BadRequestResponse]
    public async Task<IActionResult> Unmute(MonitorUnmuteRequest request)
    {
        await BusinesLayer.UnMute(request);
        return NoContent();
    }

    [HttpGet("mutes")]
    [ViewerAuthorize]
    [EndpointName("get_monitor_mutes")]
    [EndpointDescription("Get all monitor mutes")]
    [EndpointSummary("Get All Monitor Mutes")]
    [OkJsonResponse(typeof(IEnumerable<MuteItem>))]
    public async Task<ActionResult<IEnumerable<MuteItem>>> Mutes()
    {
        var result = await BusinesLayer.Mutes();
        return Ok(result);
    }
}
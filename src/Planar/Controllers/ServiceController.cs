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
[Route("service")]
public class ServiceController(ServiceDomain bl) : BaseController<ServiceDomain>(bl)
{
    [HttpGet("version")]
    [ViewerAuthorize]
    [EndpointName("get_service_version")]
    [EndpointDescription("Get service version")]
    [EndpointSummary("Get Version")]
    [OkTextResponse]
    public ActionResult<string> GetServiceVersion()
    {
        var response = BusinesLayer.GetServiceVersion();
        return Ok(response);
    }

    [HttpGet]
    [EditorAuthorize]
    [EndpointName("get_service")]
    [EndpointDescription("Get service information")]
    [EndpointSummary("Get Information")]
    [OkJsonResponse(typeof(AppSettingsInfo))]
    public async Task<ActionResult<AppSettingsInfo>> GetServiceInfo()
    {
        var response = await BusinesLayer.GetServiceInfo();
        return Ok(response);
    }

    [HttpGet("health-check")]
    [AllowAnonymous]
    [EndpointName("get_service_health_check")]
    [EndpointDescription("Service health check")]
    [EndpointSummary("Health Check")]
    [OkTextResponse]
    [ServiceUnavailableResponse]
    public async Task<ActionResult<string>> HealthCheck()
    {
        var response = await BusinesLayer.HealthCheck();
        return Ok(response);
    }

    [HttpGet("calendars")]
    [EditorAuthorize]
    [EndpointName("get_service_calendars")]
    [EndpointDescription("Get calendars list")]
    [EndpointSummary("Get Calendars")]
    [OkJsonResponse(typeof(List<string>))]
    public async Task<ActionResult<List<string>>> GetCalendars()
    {
        var list = await BusinesLayer.GetCalendars();
        return Ok(list);
    }

    [HttpPost("halt")]
    [AdministratorAuthorize]
    [EndpointName("post_service_halt")]
    [EndpointDescription("Halt (stop) service")]
    [EndpointSummary("Halt (Stop) Service")]
    [OkJsonResponse]
    public async Task<ActionResult> HaltScheduler()
    {
        await BusinesLayer.HaltScheduler();
        return Ok();
    }

    [HttpPost("start")]
    [AdministratorAuthorize]
    [EndpointName("post_service_start")]
    [EndpointDescription("Start service")]
    [EndpointSummary("Start Service")]
    [OkJsonResponse]
    public async Task<ActionResult> StartScheduler()
    {
        await BusinesLayer.StartScheduler();
        return Ok();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EndpointName("post_service_login")]
    [EndpointDescription("Login service")]
    [EndpointSummary("Login Service")]
    [JsonConsumes]
    [OkJsonResponse(typeof(LoginResponse))]
    [ConflictResponse]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await BusinesLayer.Login(request);
        return Ok(result);
    }

    [HttpGet("security-audits")]
    [AdministratorAuthorize]
    [EndpointName("get_service_security_audits")]
    [EndpointDescription("Get all security audits")]
    [EndpointSummary("Get All Security Audits")]
    [OkJsonResponse(typeof(PagingResponse<SecurityAuditModel>))]
    [BadRequestResponse]
    public async Task<ActionResult<PagingResponse<SecurityAuditModel>>> GetSecurityAudits([FromQuery] SecurityAuditsFilter request)
    {
        var result = await BusinesLayer.GetSecurityAudits(request);
        return Ok(result);
    }

    [HttpGet("working-hours/{calendar}")]
    [AdministratorAuthorize]
    [EndpointName("get_service_working_hours_calendar")]
    [EndpointDescription("Get working hours for calendar")]
    [EndpointSummary("Get Working Hours For Calendar")]
    [OkJsonResponse(typeof(WorkingHoursModel))]
    [BadRequestResponse]
    [NotFoundResponse]
    public ActionResult<WorkingHoursModel> GetWorkingHours([FromRoute][Required][MaxLength(20)] string calendar)
    {
        calendar = WebUtility.UrlDecode(calendar);
        var result = BusinesLayer.GetWorkingHours(calendar);
        return Ok(result);
    }

    [HttpGet("working-hours")]
    [AdministratorAuthorize]
    [EndpointName("get_service_working_hours")]
    [EndpointDescription("Get default working hours")]
    [EndpointSummary("Get Default Working")]
    [OkJsonResponse(typeof(IEnumerable<WorkingHoursModel>))]
    [NotFoundResponse]
    public ActionResult<IEnumerable<WorkingHoursModel>> GetDefaultWorkingHours()
    {
        var result = BusinesLayer.GetDefaultWorkingHours();
        return Ok(result);
    }
}
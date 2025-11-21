using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using Swashbuckle.AspNetCore.Annotations;
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
    [SwaggerOperation(OperationId = "get_service_version", Description = "Get service version", Summary = "Get Version")]
    [OkTextResponse]
    public ActionResult<string> GetServiceVersion()
    {
        var response = BusinesLayer.GetServiceVersion();
        return Ok(response);
    }

    [HttpGet]
    [EditorAuthorize]
    [SwaggerOperation(OperationId = "get_service", Description = "Get service information", Summary = "Get Information")]
    [OkJsonResponse(typeof(AppSettingsInfo))]
    public async Task<ActionResult<AppSettingsInfo>> GetServiceInfo()
    {
        var response = await BusinesLayer.GetServiceInfo();
        return Ok(response);
    }

    [HttpGet("health-check")]
    [AllowAnonymous]
    [SwaggerOperation(OperationId = "get_service_health_check", Description = "Service health check", Summary = "Health Check")]
    [OkTextResponse]
    [ServiceUnavailableResponse]
    public async Task<ActionResult<string>> HealthCheck()
    {
        var response = await BusinesLayer.HealthCheck();
        return Ok(response);
    }

    [HttpGet("calendars")]
    [EditorAuthorize]
    [SwaggerOperation(OperationId = "get_service_calendars", Description = "Get calendars list", Summary = "Get Calendars")]
    [OkJsonResponse(typeof(List<string>))]
    public async Task<ActionResult<List<string>>> GetCalendars()
    {
        var list = await BusinesLayer.GetCalendars();
        return Ok(list);
    }

    [HttpPost("halt")]
    [AdministratorAuthorize]
    [SwaggerOperation(OperationId = "post_service_halt", Description = "Halt (stop) service", Summary = "Halt (Stop) Service")]
    [OkJsonResponse]
    public async Task<ActionResult> HaltScheduler()
    {
        await BusinesLayer.HaltScheduler();
        return Ok();
    }

    [HttpPost("start")]
    [AdministratorAuthorize]
    [SwaggerOperation(OperationId = "post_service_start", Description = "Start service", Summary = "Start Service")]
    [OkJsonResponse]
    public async Task<ActionResult> StartScheduler()
    {
        await BusinesLayer.StartScheduler();
        return Ok();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [SwaggerOperation(OperationId = "post_service_login", Description = "Login service", Summary = "Login Service")]
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
    [SwaggerOperation(OperationId = "get_service_security_audits", Description = "Get all security audits", Summary = "Get All Security Audits")]
    [OkJsonResponse(typeof(PagingResponse<SecurityAuditModel>))]
    [BadRequestResponse]
    public async Task<ActionResult<PagingResponse<SecurityAuditModel>>> GetSecurityAudits([FromQuery] SecurityAuditsFilter request)
    {
        var result = await BusinesLayer.GetSecurityAudits(request);
        return Ok(result);
    }

    [HttpGet("working-hours/{calendar}")]
    [AdministratorAuthorize]
    [SwaggerOperation(OperationId = "get_service_working_hours_calendar", Description = "Get working hours for calendar", Summary = "Get Working Hours For Calendar")]
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
    [SwaggerOperation(OperationId = "get_service_working_hours", Description = "Get default working hours", Summary = "Get Default Working")]
    [OkJsonResponse(typeof(IEnumerable<WorkingHoursModel>))]
    [NotFoundResponse]
    public ActionResult<IEnumerable<WorkingHoursModel>> GetDefaultWorkingHours()
    {
        var result = BusinesLayer.GetDefaultWorkingHours();
        return Ok(result);
    }
}
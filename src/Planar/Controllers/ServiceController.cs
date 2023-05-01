using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [Route("service")]
    public class ServiceController : BaseController<ServiceDomain>
    {
        public ServiceController(ServiceDomain bl) : base(bl)
        {
        }

        [HttpGet]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_service", Description = "Get service information", Summary = "Get Information")]
        [OkJsonResponse(typeof(GetServiceInfoResponse))]
        public async Task<ActionResult<GetServiceInfoResponse>> GetServiceInfo()
        {
            var response = await BusinesLayer.GetServiceInfo();
            return Ok(response);
        }

        [HttpGet("{key}")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_service_key", Description = "Get service information key", Summary = "Get Information Key")]
        [OkJsonResponse(typeof(GetServiceInfoResponse))]
        public async Task<ActionResult<string>> GetServiceInfo(string key)
        {
            var response = await BusinesLayer.GetServiceInfo(key);
            return Ok(response);
        }

        [HttpGet("healthCheck")]
        [AllowAnonymous]
        [SwaggerOperation(OperationId = "get_service_healthcheck", Description = "Service health check", Summary = "Health Check")]
        [OkJsonResponse(typeof(GetServiceInfoResponse))]
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
    }
}
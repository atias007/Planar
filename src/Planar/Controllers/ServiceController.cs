using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Service.API;
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
        public async Task<ActionResult<GetServiceInfoResponse>> GetServiceInfo()
        {
            var response = await BusinesLayer.GetServiceInfo();
            return Ok(response);
        }

        [HttpGet("{key}")]
        public async Task<ActionResult<string>> GetServiceInfo(string key)
        {
            var response = await BusinesLayer.GetServiceInfo(key);
            return Ok(response);
        }

        [HttpGet("healthCheck")]
        public async Task<ActionResult<string>> HealthCheck()
        {
            var response = await BusinesLayer.HealthCheck();
            return Ok(response);
        }

        [HttpGet("calendars")]
        public async Task<ActionResult<List<string>>> GetCalendars()
        {
            var list = await BusinesLayer.GetCalendars();
            return Ok(list);
        }

        [HttpPost("stop")]
        public async Task<ActionResult> StopScheduler()
        {
            await BusinesLayer.StopScheduler();
            return Ok();
        }

        [HttpPost("start")]
        public async Task<ActionResult> StartScheduler()
        {
            await BusinesLayer.StartScheduler();
            return Ok();
        }

        [HttpPost("login")]
        [JsonConsumes]
        public async Task<ActionResult<string>> Login([FromBody] LoginRequest request)
        {
            var result = await BusinesLayer.Login(request);
            return Ok(result);
        }
    }
}
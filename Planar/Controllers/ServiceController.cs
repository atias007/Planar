using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.API;
using Planar.Service.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [ApiController]
    [Route("service")]
    public class ServiceController : BaseController<ServiceController, ServiceDomain>
    {
        public ServiceController(ILogger<ServiceController> logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        [HttpGet]
        public async Task<ActionResult<GetServiceInfoResponse>> GetServiceInfo()
        {
            var response = await BusinesLayer.GetServiceInfo();
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

        [HttpGet("nodes")]
        public async Task<ActionResult<List<ClusterNode>>> GetNodes()
        {
            var response = await BusinesLayer.GetNodes();
            return Ok(response);
        }
    }
}
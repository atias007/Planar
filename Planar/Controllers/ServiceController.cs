using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service;
using Planar.Service.API;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public ActionResult<GetServiceInfoResponse> GetServiceInfo()
        {
            var response = new GetServiceInfoResponse
            {
                InStandbyMode = Scheduler.InStandbyMode,
                IsShutdown = Scheduler.IsShutdown,
                IsStarted = Scheduler.IsStarted,
                SchedulerInstanceId = Scheduler.SchedulerInstanceId,
                SchedulerName = Scheduler.SchedulerName,
                Environment = Global.Environment,
            };

            return Ok(response);
        }

        [HttpGet("calendars")]
        public async Task<ActionResult<List<string>>> GetCalendars()
        {
            var list = (await Scheduler.GetCalendarNames()).ToList();
            return Ok(list);
        }

        [HttpPost("stop")]
        public async Task<ActionResult> StopScheduler(StopSchedulerRequest request)
        {
            ValidateEntity(request);
            await Scheduler.Shutdown(request.WaitJobsToComplete);

            var t = Task.Run(async () =>
            {
                await Task.Delay(3000);
                MainService.Shutdown();
            });

            return Ok();
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Planner.API.Common.Entities;
using Planner.Common;
using Planner.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planner.Controllers
{
    [ApiController]
    [Route("service")]
    public class ServiceController : BaseController<ServiceController, null>
    {
        public ServiceController(ILogger<ServiceController> logger) : base(logger, null)
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
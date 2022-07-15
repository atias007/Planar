using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service;
using Planar.Service.API;
using Quartz;
using Quartz.Impl.Matchers;
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
        public async Task<ActionResult<GetServiceInfoResponse>> GetServiceInfo()
        {
            var totalJobs = Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            var totalGroups = Scheduler.GetJobGroupNames();
            var metadata = Scheduler.GetMetaData();

            var response = new GetServiceInfoResponse
            {
                InStandbyMode = Scheduler.InStandbyMode,
                IsShutdown = Scheduler.IsShutdown,
                IsStarted = Scheduler.IsStarted,
                SchedulerInstanceId = Scheduler.SchedulerInstanceId,
                SchedulerName = Scheduler.SchedulerName,
                Environment = Global.Environment,
                TotalJobs = (await totalJobs).Count,
                TotalGroups = (await totalGroups).Count,
                Clustered = (await metadata).JobStoreClustered,
                JobStoreType = (await metadata).JobStoreType.FullName,
                RunningSince = (await metadata).RunningSince.GetValueOrDefault().DateTime,
                QuartzVersion = (await metadata).Version
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
        public async Task<ActionResult> StopScheduler()
        {
            await Scheduler.Standby();
            return Ok();
        }

        [HttpPost("start")]
        public async Task<ActionResult> StartScheduler()
        {
            await Scheduler.Start();
            return Ok();
        }
    }
}
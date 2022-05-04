using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.API;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [ApiController]
    [Route("job")]
    public class JobController : BaseController<JobController, JobDomain>
    {
        public JobController(ILogger<JobController> logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        [HttpPost]
        public async Task<ActionResult<string>> Add([FromBody] AddJobRequest request)
        {
            var result = await BusinesLayer.Add(request);
            return CreatedAtAction(nameof(Get), new { result }, result);
        }

        [HttpGet]
        public async Task<ActionResult<List<JobRowDetails>>> GetAll()
        {
            var result = await BusinesLayer.GetAll();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Remove([FromRoute] string id)
        {
            await BusinesLayer.Remove(id);
            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<JobDetails>> Get([FromRoute] string id)
        {
            var result = await BusinesLayer.Get(id);
            return Ok(result);
        }

        [HttpPost("data")]
        public async Task<IActionResult> UpsertData([FromBody] JobDataRequest request)
        {
            await BusinesLayer.UpsertData(request);
            return CreatedAtAction(nameof(Get), new { request.Id }, null);
        }

        [HttpPost("invoke")]
        public async Task<IActionResult> Invoke([FromBody] InvokeJobRequest request)
        {
            await BusinesLayer.Invoke(request);
            return Accepted();
        }

        [HttpPost("pause")]
        public IActionResult Pause([FromBody] JobOrTriggerKey request)
        {
            return Ok();
        }

        [HttpPost("resume")]
        public IActionResult Resume([FromBody] JobOrTriggerKey request)
        {
            return Ok();
        }

        [HttpPost("stop")]
        public IActionResult Stop([FromBody] FireInstanceIdRequest request)
        {
            return Ok();
        }

        [HttpPost("pauseAll")]
        public IActionResult PauseAll()
        {
            return Ok();
        }

        [HttpPost("resumeAll")]
        public IActionResult ResumeAll()
        {
            return Ok();
        }

        [HttpGet("{id}/settings")]
        public ActionResult<Dictionary<string, string>> GetSettings([FromRoute] string id)
        {
            return Ok();
        }

        [HttpGet("running/{instanceId}")]
        public ActionResult<List<RunningJobDetails>> GetRunning([FromRoute] string instanceId)
        {
            return Ok();
        }

        [HttpGet("runningInfo/{instanceId}")]
        public ActionResult<object> GetRunningInfo([FromRoute] string instanceId)
        {
            return Ok();
        }

        [HttpGet("testStatus/{id}")]
        public ActionResult<GetTestStatusResponse> GetTestStatus([FromRoute] int id)
        {
            return Ok();
        }

        [HttpDelete("{id}/data/{key}")]
        public IActionResult RemoveData([FromRoute] string id, [FromRoute] string key)
        {
            return Ok();
        }

        [HttpDelete("{id}/allData")]
        public IActionResult ClearData([FromRoute] string id)
        {
            return Ok();
        }

        [HttpPut("property")]
        public IActionResult UpsertProperty([FromBody] UpsertJobPropertyRequest request)
        {
            return Ok();
        }

        [HttpGet("{id}/lastInstanceId")]
        public IActionResult GetLastInstanceId([FromBody] UpsertJobPropertyRequest request, [FromQuery] DateTime invokeDate)
        {
            return Ok();
        }
    }
}
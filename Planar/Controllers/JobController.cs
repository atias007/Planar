using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.API;
using Planar.Validation.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
        public async Task<ActionResult<JobIdResponse>> Add([FromBody] AddJobRequest request)
        {
            var result = await BusinesLayer.Add(request);
            return CreatedAtAction(nameof(Get), result, result);
        }

        [HttpGet]
        public async Task<ActionResult<List<JobRowDetails>>> GetAll([FromQuery] GetAllJobsRequest request)
        {
            var result = await BusinesLayer.GetAll(request);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Remove([FromRoute][Required] string id)
        {
            await BusinesLayer.Remove(id);
            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<JobDetails>> Get([FromRoute][Required] string id)
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
        public async Task<IActionResult> Pause([FromBody] JobOrTriggerKey request)
        {
            await BusinesLayer.Pause(request);
            return Accepted();
        }

        [HttpPost("pauseAll")]
        public async Task<IActionResult> PauseAll()
        {
            await BusinesLayer.PauseAll();
            return Accepted();
        }

        [HttpPost("resume")]
        public async Task<IActionResult> Resume([FromBody] JobOrTriggerKey request)
        {
            await BusinesLayer.Resume(request);
            return Accepted();
        }

        [HttpPost("resumeAll")]
        public async Task<IActionResult> ResumeAll()
        {
            await BusinesLayer.ResumeAll();
            return Accepted();
        }

        [HttpPost("stop")]
        public async Task<IActionResult> Stop([FromBody] FireInstanceIdRequest request)
        {
            await BusinesLayer.Stop(request);
            return Accepted();
        }

        [HttpGet("{id}/settings")]
        public async Task<ActionResult<Dictionary<string, string>>> GetSettings([FromRoute][Required] string id)
        {
            var result = await BusinesLayer.GetSettings(id);
            return Ok(result);
        }

        [HttpGet("running/{instanceId}")]
        public async Task<ActionResult<RunningJobDetails>> GetAllRunning([FromRoute][Required] string instanceId)
        {
            var result = await BusinesLayer.GetRunning(instanceId);
            return Ok(result);
        }

        [HttpGet("running")]
        public async Task<ActionResult<List<RunningJobDetails>>> GetRunning()
        {
            var result = await BusinesLayer.GetRunning();
            return Ok(result);
        }

        [HttpGet("runningInfo/{instanceId}")]
        public async Task<ActionResult<GetRunningInfoResponse>> GetRunningInfo([FromRoute][Required] string instanceId)
        {
            var result = await BusinesLayer.GetRunningInfo(instanceId);
            return Ok(result);
        }

        [HttpGet("testStatus/{id}")]
        public async Task<ActionResult<GetTestStatusResponse>> GetTestStatus([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetTestStatus(id);
            return Ok(result);
        }

        [HttpDelete("{id}/data/{key}")]
        public async Task<IActionResult> RemoveData([FromRoute][Required] string id, [FromRoute][Required] string key)
        {
            await BusinesLayer.RemoveData(id, key);
            return NoContent();
        }

        [HttpDelete("{id}/allData")]
        public async Task<IActionResult> ClearData([FromRoute][Required] string id)
        {
            await BusinesLayer.ClearData(id);
            return NoContent();
        }

        [HttpPut("property")]
        public async Task<IActionResult> UpdateProperty([FromBody] UpsertJobPropertyRequest request)
        {
            await BusinesLayer.UpdateProperty(request);
            return CreatedAtAction(nameof(Get), new { request.Id }, request.Id);
        }

        [HttpGet("{id}/lastInstanceId")]
        public async Task<ActionResult<LastInstanceId>> GetLastInstanceId([FromRoute][Required] string id, [FromQuery] DateTime invokeDate)
        {
            var result = await BusinesLayer.GetLastInstanceId(id, invokeDate);
            return Ok(result);
        }
    }
}
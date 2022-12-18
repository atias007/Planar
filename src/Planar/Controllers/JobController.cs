using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Service.API;
using Planar.Validation.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [ApiController]
    [Route("job")]
    public class JobController : BaseController<JobDomain>
    {
        public JobController(JobDomain bl) : base(bl)
        {
        }

        [HttpPost("planar")]
        [JsonConsumes]
        [CreatedResponse(typeof(JobIdResponse))]
        public async Task<ActionResult<JobIdResponse>> AddPlanar([FromBody] SetJobRequest<PlanarJobProperties> request)
        {
            var result = await BusinesLayer.Add(request);
            return CreatedAtAction(nameof(Get), result, result);
        }

        [HttpPut("planar")]
        [JsonConsumes]
        [CreatedResponse(typeof(JobIdResponse))]
        public async Task<ActionResult<JobIdResponse>> UpdatePlanar([FromBody] UpdateJobRequest<PlanarJobProperties> request)
        {
            var result = await BusinesLayer.Update(request);
            return CreatedAtAction(nameof(Get), result, result);
        }

        [HttpPost("folder")]
        [JsonConsumes]
        [CreatedResponse(typeof(JobIdResponse))]
        public async Task<ActionResult<JobIdResponse>> AddByFolder([FromBody] SetJobFoldeRequest request)
        {
            var result = await BusinesLayer.AddByFolder(request);
            return CreatedAtAction(nameof(Get), result, result);
        }

        [HttpPut("folder")]
        [JsonConsumes]
        [CreatedResponse(typeof(JobIdResponse))]
        public async Task<ActionResult<JobIdResponse>> UpdateByFolder([FromBody] UpdateJobFolderRequest request)
        {
            var result = await BusinesLayer.UpdateByFolder(request);
            return CreatedAtAction(nameof(Get), result, result);
        }

        [HttpGet]
        [OkJsonResponse(typeof(List<JobRowDetails>))]
        public async Task<ActionResult<List<JobRowDetails>>> GetAll([FromQuery] GetAllJobsRequest request)
        {
            var result = await BusinesLayer.GetAll(request);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [NoContentResponse]
        public async Task<IActionResult> Remove([FromRoute][Required] string id)
        {
            await BusinesLayer.Remove(id);
            return NoContent();
        }

        [HttpGet("{id}")]
        [OkJsonResponse(typeof(JobDetails))]
        public async Task<ActionResult<JobDetails>> Get([FromRoute][Required] string id)
        {
            var result = await BusinesLayer.Get(id);
            return Ok(result);
        }

        [HttpGet("nextRunning/{id}")]
        [OkTextResponse]
        public async Task<ActionResult<string>> GetNextRunning([FromRoute][Required] string id)
        {
            var result = await BusinesLayer.GetNextRunning(id);
            return Ok(result);
        }

        [HttpGet("prevRunning/{id}")]
        [OkTextResponse]
        public async Task<ActionResult<string>> GetPreviousRunning([FromRoute][Required] string id)
        {
            var result = await BusinesLayer.GetPreviousRunning(id);
            return Ok(result);
        }

        [HttpPost("data")]
        [JsonConsumes]
        [CreatedResponse]
        public async Task<IActionResult> AddData([FromBody] JobOrTriggerDataRequest request)
        {
            await BusinesLayer.UpsertData(request, JobDomain.UpsertMode.Add);
            return CreatedAtAction(nameof(Get), new { request.Id }, null);
        }

        [HttpPut("data")]
        [JsonConsumes]
        [CreatedResponse]
        public async Task<IActionResult> UpdateData([FromBody] JobOrTriggerDataRequest request)
        {
            await BusinesLayer.UpsertData(request, JobDomain.UpsertMode.Update);
            return CreatedAtAction(nameof(Get), new { request.Id }, null);
        }

        [HttpDelete("{id}/data/{key}")]
        [NoContentResponse]
        public async Task<IActionResult> RemoveData([FromRoute][Required] string id, [FromRoute][Required] string key)
        {
            await BusinesLayer.RemoveData(id, key);
            return NoContent();
        }

        [HttpPost("invoke")]
        [JsonConsumes]
        [AcceptedContentResponse]
        public async Task<IActionResult> Invoke([FromBody] InvokeJobRequest request)
        {
            await BusinesLayer.Invoke(request);
            return Accepted();
        }

        [HttpPost("pause")]
        [JsonConsumes]
        [AcceptedContentResponse]
        public async Task<IActionResult> Pause([FromBody] JobOrTriggerKey request)
        {
            await BusinesLayer.Pause(request);
            return Accepted();
        }

        [HttpPost("pauseAll")]
        [AcceptedContentResponse]
        public async Task<IActionResult> PauseAll()
        {
            await BusinesLayer.PauseAll();
            return Accepted();
        }

        [HttpPost("resume")]
        [JsonConsumes]
        [AcceptedContentResponse]
        public async Task<IActionResult> Resume([FromBody] JobOrTriggerKey request)
        {
            await BusinesLayer.Resume(request);
            return Accepted();
        }

        [HttpPost("resumeAll")]
        [AcceptedContentResponse]
        public async Task<IActionResult> ResumeAll()
        {
            await BusinesLayer.ResumeAll();
            return Accepted();
        }

        [HttpPost("stop")]
        [JsonConsumes]
        [AcceptedContentResponse]
        public async Task<ActionResult<bool>> Stop([FromBody] FireInstanceIdRequest request)
        {
            await BusinesLayer.Stop(request);
            return Accepted();
        }

        [HttpGet("{id}/settings")]
        [OkJsonResponse(typeof(IEnumerable<KeyValueItem>))]
        public async Task<ActionResult<IEnumerable<KeyValueItem>>> GetSettings([FromRoute][Required] string id)
        {
            var result = await BusinesLayer.GetSettings(id);
            return Ok(result);
        }

        [HttpGet("running/{instanceId}")]
        [OkJsonResponse(typeof(RunningJobDetails))]
        public async Task<ActionResult<RunningJobDetails>> GetAllRunning([FromRoute][Required] string instanceId)
        {
            var result = await BusinesLayer.GetRunning(instanceId);
            return Ok(result);
        }

        [HttpGet("running")]
        [OkJsonResponse(typeof(List<RunningJobDetails>))]
        public async Task<ActionResult<List<RunningJobDetails>>> GetRunning()
        {
            var result = await BusinesLayer.GetRunning();
            return Ok(result);
        }

        [HttpGet("runningData/{instanceId}")]
        [OkJsonResponse(typeof(GetRunningDataResponse))]
        public async Task<ActionResult<GetRunningDataResponse>> GetRunningData([FromRoute][Required] string instanceId)
        {
            var result = await BusinesLayer.GetRunningData(instanceId);
            return Ok(result);
        }

        [HttpGet("testStatus/{id}")]
        [OkJsonResponse(typeof(GetTestStatusResponse))]
        public async Task<ActionResult<GetTestStatusResponse>> GetTestStatus([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetTestStatus(id);
            return Ok(result);
        }

        [HttpGet("{id}/lastInstanceId")]
        [OkJsonResponse(typeof(LastInstanceId))]
        public async Task<ActionResult<LastInstanceId>> GetLastInstanceId([FromRoute][Required] string id, [FromQuery] DateTime invokeDate)
        {
            var result = await BusinesLayer.GetLastInstanceId(id, invokeDate);
            return Ok(result);
        }
    }
}
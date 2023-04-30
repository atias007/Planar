using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using Planar.Validation.Attributes;
using Swashbuckle.AspNetCore.Annotations;
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
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "post_job_planar_add", Description = "Add new planar job", Summary = "Add Planar Job")]
        [JsonConsumes]
        [CreatedResponse(typeof(JobIdResponse))]
        [BadRequestResponse]
        [ConflictResponse]
        public async Task<ActionResult<JobIdResponse>> AddPlanar([FromBody] SetJobRequest<PlanarJobProperties> request)
        {
            var result = await BusinesLayer.Add(request);
            return CreatedAtAction(nameof(Get), result, result);
        }

        [HttpPost("process")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "post_job_process_add", Description = "Add new process job", Summary = "Add Process Job")]
        [JsonConsumes]
        [CreatedResponse(typeof(JobIdResponse))]
        [BadRequestResponse]
        [ConflictResponse]
        public async Task<ActionResult<JobIdResponse>> AddProcess([FromBody] SetJobRequest<ProcessJobProperties> request)
        {
            var result = await BusinesLayer.Add(request);
            return CreatedAtAction(nameof(Get), result, result);
        }

        [HttpPost("sql")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "post_job_sql_add", Description = "Add new sql job", Summary = "Add Sql Job")]
        [JsonConsumes]
        [CreatedResponse(typeof(JobIdResponse))]
        [BadRequestResponse]
        [ConflictResponse]
        public async Task<ActionResult<JobIdResponse>> AddSql([FromBody] SetJobRequest<SqlJobProperties> request)
        {
            var result = await BusinesLayer.Add(request);
            return CreatedAtAction(nameof(Get), result, result);
        }

        [HttpPut("planar")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "put_job_planar", Description = "Update existing planar job", Summary = "Update Planar Job")]
        [JsonConsumes]
        [CreatedResponse(typeof(JobIdResponse))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<JobIdResponse>> UpdatePlanar([FromBody] UpdateJobRequest<PlanarJobProperties> request)
        {
            var result = await BusinesLayer.Update(request);
            return CreatedAtAction(nameof(Get), result, result);
        }

        [HttpPut("process")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "put_job_process", Description = "Update existing process job", Summary = "Update Process Job")]
        [JsonConsumes]
        [CreatedResponse(typeof(JobIdResponse))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<JobIdResponse>> UpdateProcess([FromBody] UpdateJobRequest<ProcessJobProperties> request)
        {
            var result = await BusinesLayer.Update(request);
            return CreatedAtAction(nameof(Get), result, result);
        }

        [HttpPut("sql")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "put_job_sql", Description = "Update existing sql job", Summary = "Update Sql Job")]
        [JsonConsumes]
        [CreatedResponse(typeof(JobIdResponse))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<JobIdResponse>> UpdateSql([FromBody] UpdateJobRequest<SqlJobProperties> request)
        {
            var result = await BusinesLayer.Update(request);
            return CreatedAtAction(nameof(Get), result, result);
        }

        [HttpPost("path")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "post_job_folder", Description = "Add job by yml job file", Summary = "Add Job By Yml")]
        [JsonConsumes]
        [CreatedResponse(typeof(JobIdResponse))]
        [BadRequestResponse]
        [ConflictResponse]
        public async Task<ActionResult<JobIdResponse>> AddByPath([FromBody] SetJobPathRequest request)
        {
            var result = await BusinesLayer.AddByPath(request);
            return CreatedAtAction(nameof(Get), result, result);
        }

        [HttpPut("folder")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "put_job_folder", Description = "Update job by yml job file", Summary = "Update Job By Yml")]
        [JsonConsumes]
        [CreatedResponse(typeof(JobIdResponse))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<JobIdResponse>> UpdateByPath([FromBody] UpdateJobPathRequest request)
        {
            var result = await BusinesLayer.UpdateByPath(request);
            return CreatedAtAction(nameof(Get), result, result);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("available-jobs")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_job_available_jobs", Description = "", Summary = "")]
        [OkJsonResponse(typeof(List<AvailableJobToAdd>))]
        public async Task<ActionResult<List<AvailableJobToAdd>>> GetAvailableJobsToAdd()
        {
            var result = await BusinesLayer.GetAvailableJobsToAdd();
            return Ok(result);
        }

        [HttpGet]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_job", Description = "Get all jobs", Summary = "Get All Jobs")]
        [OkJsonResponse(typeof(List<JobRowDetails>))]
        public async Task<ActionResult<List<JobRowDetails>>> GetAll([FromQuery] GetAllJobsRequest request)
        {
            var result = await BusinesLayer.GetAll(request);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "delete_job_id", Description = "Delete job", Summary = "Delete Job")]
        [NoContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<IActionResult> Remove([FromRoute][Required] string id)
        {
            await BusinesLayer.Remove(id);
            return NoContent();
        }

        [HttpGet("{id}")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_job_id", Description = "Get job details by id", Summary = "Get Job By Id")]
        [OkJsonResponse(typeof(JobDetails))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<JobDetails>> Get([FromRoute][Required] string id)
        {
            var result = await BusinesLayer.Get(id);
            return Ok(result);
        }

        [HttpGet("nextRunning/{id}")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_job_nextRunning_id", Description = "Get the next running date & time of job", Summary = "Get Next Running Date")]
        [OkTextResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<string>> GetNextRunning([FromRoute][Required] string id)
        {
            var result = await BusinesLayer.GetNextRunning(id);
            return Ok(result);
        }

        [HttpGet("prevRunning/{id}")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_job_prevRunning_id", Description = "Get the previous running date & time of job", Summary = "Get Previous Running Date")]
        [OkTextResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<string>> GetPreviousRunning([FromRoute][Required] string id)
        {
            var result = await BusinesLayer.GetPreviousRunning(id);
            return Ok(result);
        }

        [HttpPost("data")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "post_job_data", Description = "Add job data", Summary = "Add Job Data")]
        [JsonConsumes]
        [CreatedResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<IActionResult> AddData([FromBody] JobOrTriggerDataRequest request)
        {
            await BusinesLayer.PutData(request, JobDomain.PutMode.Add);
            return CreatedAtAction(nameof(Get), new { request.Id }, null);
        }

        [HttpPut("data")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "put_job_data", Description = "Update job data", Summary = "Update Job Data")]
        [JsonConsumes]
        [CreatedResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<IActionResult> UpdateData([FromBody] JobOrTriggerDataRequest request)
        {
            await BusinesLayer.PutData(request, JobDomain.PutMode.Update);
            return CreatedAtAction(nameof(Get), new { request.Id }, null);
        }

        [HttpDelete("{id}/data/{key}")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "delete_job_id_data_key", Description = "Delete job data", Summary = "Delete Job Data")]
        [NoContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<IActionResult> RemoveData([FromRoute][Required] string id, [FromRoute][Required] string key)
        {
            await BusinesLayer.RemoveData(id, key);
            return NoContent();
        }

        [HttpPost("invoke")]
        [TesterAuthorize]
        [SwaggerOperation(OperationId = "post_job_invoke", Description = "Invoke job", Summary = "Invoke Job")]
        [JsonConsumes]
        [AcceptedContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<IActionResult> Invoke([FromBody] InvokeJobRequest request)
        {
            await BusinesLayer.Invoke(request);
            return Accepted();
        }

        [HttpPost("pause")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "post_job_pause", Description = "Pause job", Summary = "Pause Job")]
        [JsonConsumes]
        [AcceptedContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<IActionResult> Pause([FromBody] JobOrTriggerKey request)
        {
            await BusinesLayer.Pause(request);
            return Accepted();
        }

        [HttpPost("pauseAll")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "post_job_pauseall", Description = "Pause all jobs", Summary = "Pause All Jobs")]
        [AcceptedContentResponse]
        public async Task<IActionResult> PauseAll()
        {
            await BusinesLayer.PauseAll();
            return Accepted();
        }

        [HttpPost("resume")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "post_job_resume", Description = "Resume job", Summary = "Resume Job")]
        [JsonConsumes]
        [AcceptedContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<IActionResult> Resume([FromBody] JobOrTriggerKey request)
        {
            await BusinesLayer.Resume(request);
            return Accepted();
        }

        [HttpPost("resumeAll")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "post_job_resumeall", Description = "Resume all jobs", Summary = "Resume All Jobs")]
        [AcceptedContentResponse]
        public async Task<IActionResult> ResumeAll()
        {
            await BusinesLayer.ResumeAll();
            return Accepted();
        }

        [HttpPost("cancel")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "post_job_cancel", Description = "Cancel running job", Summary = "Cancel Job")]
        [JsonConsumes]
        [AcceptedContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<bool>> Cancel([FromBody] FireInstanceIdRequest request)
        {
            await BusinesLayer.Cancel(request);
            return Accepted();
        }

        [HttpGet("{id}/settings")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "gat_job_id_settings", Description = "Get job settings", Summary = "Get Job Settings")]
        [OkJsonResponse(typeof(IEnumerable<KeyValueItem>))]
        [BadRequestResponse]
        public async Task<ActionResult<IEnumerable<KeyValueItem>>> GetSettings([FromRoute][Required] string id)
        {
            var result = await BusinesLayer.GetSettings(id);
            return Ok(result);
        }

        [HttpGet("running/{instanceId}")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_job_running_instanceid", Description = "Get runnng job info", Summary = "Get Runnng Job Info")]
        [OkJsonResponse(typeof(RunningJobDetails))]
        [BadRequestResponse]
        public async Task<ActionResult<RunningJobDetails>> GetAllRunning([FromRoute][Required] string instanceId)
        {
            var result = await BusinesLayer.GetRunning(instanceId);
            return Ok(result);
        }

        [HttpGet("running")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_job_running", Description = "Gat all running jobs", Summary = "Gat All Running Jobs")]
        [OkJsonResponse(typeof(List<RunningJobDetails>))]
        public async Task<ActionResult<List<RunningJobDetails>>> GetRunning()
        {
            var result = await BusinesLayer.GetRunning();
            return Ok(result);
        }

        [HttpGet("runningData/{instanceId}")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_job_runningData_instanceid", Description = "Get running job log & exception", Summary = "Get Running Job Data")]
        [OkJsonResponse(typeof(GetRunningDataResponse))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<GetRunningDataResponse>> GetRunningData([FromRoute][Required] string instanceId)
        {
            var result = await BusinesLayer.GetRunningData(instanceId);
            return Ok(result);
        }

        [HttpGet("jobfile/{name}")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_job_jobfile_name", Description = "Get JobFile.yml template", Summary = "Get JobFile.yml Template")]
        [OkYmlResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public ActionResult<string> GetJobFileTemplate([Required][FromRoute] string name)
        {
            var result = BusinesLayer.GetJobFileTemplate(name);
            return Ok(result);
        }

        [HttpGet("types")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_job_types", Description = "Get all job types", Summary = "Get All Job Types")]
        [OkJsonResponse(typeof(IEnumerable<string>))]
        public ActionResult<IEnumerable<string>> GetJobTypes()
        {
            var result = JobDomain.GetJobTypes();
            return Ok(result);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("testStatus/{id}")]
        [TesterAuthorize]
        [SwaggerOperation(OperationId = "get_job_teststatus_id", Description = "", Summary = "")]
        [OkJsonResponse(typeof(GetTestStatusResponse))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<GetTestStatusResponse>> GetTestStatus([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetTestStatus(id);
            return Ok(result);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("{id}/lastInstanceId")]
        [TesterAuthorize]
        [SwaggerOperation(OperationId = "get_job_id_lastinstanceid", Description = "", Summary = "")]
        [OkJsonResponse(typeof(LastInstanceId))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<LastInstanceId>> GetLastInstanceId([FromRoute][Required] string id, [FromQuery] DateTime invokeDate)
        {
            var result = await BusinesLayer.GetLastInstanceId(id, invokeDate);
            return Ok(result);
        }
    }
}
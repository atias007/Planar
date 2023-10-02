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

        [HttpPost("folder")]
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
        [OkJsonResponse(typeof(PagingResponse<JobBasicDetails>))]
        public async Task<ActionResult<PagingResponse<JobBasicDetails>>> GetAll([FromQuery] GetAllJobsRequest request)
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

        [HttpGet("{id}/info")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_job_info_id", Description = "Get job info by id", Summary = "Get Job Info By Id")]
        [OkJsonResponse(typeof(JobDescription))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<JobDetails>> GetDescription([FromRoute][Required] string id)
        {
            var result = await BusinesLayer.GetDescription(id);
            return Ok(result);
        }

        [HttpGet("{id}/next-running")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_job_next_running_id", Description = "Get the next running date & time of job", Summary = "Get Next Running Date")]
        [OkTextResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<DateTime?>> GetNextRunning([FromRoute][Required] string id)
        {
            var result = await BusinesLayer.GetNextRunning(id);
            return Ok(result);
        }

        [HttpGet("{id}/prev-running")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_job_prev_running_id", Description = "Get the previous running date & time of job", Summary = "Get Previous Running Date")]
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

        [HttpPost("queue-invoke")]
        [TesterAuthorize]
        [SwaggerOperation(OperationId = "post_job_queue_invoke", Description = "Queue invokation of job", Summary = "Queue Invokation Of Job")]
        [JsonConsumes]
        [AcceptedContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<IActionResult> QueueInvoke([FromBody] QueueInvokeJobRequest request)
        {
            await BusinesLayer.QueueInvoke(request);
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

        [HttpPost("pause-all")]
        [AdministratorAuthorize]
        [SwaggerOperation(OperationId = "post_job_pause_all", Description = "Pause all jobs", Summary = "Pause All Jobs")]
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

        [HttpPost("resume-all")]
        [AdministratorAuthorize]
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

        [HttpGet("running-instance/{instanceId}")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_job_running_instanceid", Description = "Get runnng job info", Summary = "Get Runnng Job Info")]
        [OkJsonResponse(typeof(RunningJobDetails))]
        [BadRequestResponse]
        public async Task<ActionResult<RunningJobDetails>> GetRunningInstance([FromRoute][Required] string instanceId)
        {
            var result = await BusinesLayer.GetRunning(instanceId);
            return Ok(result);
        }

        [HttpGet("running")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_job_running", Description = "Gat all running jobs", Summary = "Gat All Running Jobs")]
        [OkJsonResponse(typeof(List<RunningJobDetails>))]
        public async Task<ActionResult<List<RunningJobDetails>>> GetAllRunning()
        {
            var result = await BusinesLayer.GetRunning();
            return Ok(result);
        }

        [HttpGet("{instanceId}/running-data")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_job_running_data_instanceid", Description = "Get running job log & exception", Summary = "Get Running Job Data")]
        [OkJsonResponse(typeof(RunningJobData))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<RunningJobData>> GetRunningData([FromRoute][Required] string instanceId)
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
        [HttpGet("{id}/test-status")]
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
        [HttpGet("{id}/last-instance-id")]
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

        [HttpGet("{id}/audit")]
        [AdministratorAuthorize]
        [SwaggerOperation(OperationId = "get_job_id_audit", Description = "Get audits for job", Summary = "Get Audits For Job")]
        [OkJsonResponse(typeof(PagingResponse<JobAuditDto>))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<PagingResponse<JobAuditDto>>> GetJobAudit([FromRoute][Required] string id, [FromQuery] PagingRequest paging)
        {
            var result = await BusinesLayer.GetJobAudits(id, paging);
            return Ok(result);
        }

        [HttpGet("audit/{auditId}")]
        [AdministratorAuthorize]
        [SwaggerOperation(OperationId = "get_job_audit_audit_id", Description = "Get audit by id", Summary = "Get Audit By Id")]
        [OkJsonResponse(typeof(JobAuditWithInfoDto))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<JobAuditWithInfoDto>> GetJobAudit([FromRoute][Id] int auditId)
        {
            var result = await BusinesLayer.GetJobAudit(auditId);
            return Ok(result);
        }

        [HttpGet("audits")]
        [AdministratorAuthorize]
        [SwaggerOperation(OperationId = "get_job_audits", Description = "Get all audits", Summary = "Get All Audits")]
        [BadRequestResponse]
        [OkJsonResponse(typeof(PagingResponse<JobAuditDto>))]
        public async Task<ActionResult<PagingResponse<JobAuditDto>>> GetJobAudits([FromQuery] PagingRequest request)
        {
            var result = await BusinesLayer.GetAudits(request);
            return Ok(result);
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using Planar.Validation.Attributes;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Controllers;

[ApiController]
[Route("job")]
public class JobController(JobDomain bl) : BaseController<JobDomain>(bl)
{
    [HttpPost]
    [EditorAuthorize]
    [SwaggerOperation(OperationId = "post_job", Description = "Add job by yml file", Summary = "Add Job By Yml File")]
    [JsonConsumes]
    [CreatedResponse(typeof(PlanarIdResponse))]
    [BadRequestResponse]
    [ConflictResponse]
    public async Task<ActionResult<PlanarIdResponse>> Add([FromBody] SetJobPathRequest request)
    {
        var result = await BusinesLayer.Add(request);
        return CreatedAtAction(nameof(Get), result, result);
    }

    [HttpPut]
    [EditorAuthorize]
    [SwaggerOperation(OperationId = "put_job_id", Description = "Update job", Summary = "Update Job")]
    [JsonConsumes]
    [CreatedResponse(typeof(PlanarIdResponse))]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<ActionResult<PlanarIdResponse>> Update(UpdateJobRequest request)
    {
        var result = await BusinesLayer.Update(request);
        return CreatedAtAction(nameof(Get), result, result);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("available-jobs")]
    [EditorAuthorize]
    [SwaggerOperation(OperationId = "get_job_available_jobs_mode", Description = "", Summary = "")]
    [OkJsonResponse(typeof(List<AvailableJob>))]
    public async Task<ActionResult<List<AvailableJob>>> GetAvailableJobs([FromQuery] bool update)
    {
        var result = await BusinesLayer.GetAvailableJobs(update);
        return Ok(result);
    }

    [HttpGet]
    [ViewerAuthorize]
    [BadRequestResponse]
    [SwaggerOperation(OperationId = "get_job", Description = "Get all jobs", Summary = "Get All Jobs")]
    [OkJsonResponse(typeof(PagingResponse<JobBasicDetails>))]
    public async Task<ActionResult<PagingResponse<JobBasicDetails>>> GetAll([FromQuery] GetAllJobsRequest request)
    {
        var result = await BusinesLayer.GetAll(request);
        return Ok(result);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("jobfilename/{id}")]
    [ViewerAuthorize]
    [BadRequestResponse]
    [NotFoundResponse]
    [SwaggerOperation(OperationId = "get_job_file_id", Description = "Get JobFile.yml filename", Summary = "Get JobFile.yml Filename")]
    [OkTextResponse]
    public async Task<ActionResult<string>> GetJobFilename([FromRoute][Required] string id)
    {
        var result = await BusinesLayer.GetJobFilename(id);
        return Ok(result);
    }

    [HttpGet("groups")]
    [ViewerAuthorize]
    [SwaggerOperation(OperationId = "get_groups", Description = "Get job groups", Summary = "Get Job Groups")]
    [OkJsonResponse(typeof(IEnumerable<string>))]
    public async Task<ActionResult<IEnumerable<string>>> GetGroupNames()
    {
        var result = await BusinesLayer.GetJobGroupNames();
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
        key = WebUtility.UrlDecode(key);
        await BusinesLayer.RemoveData(id, key);
        return NoContent();
    }

    [HttpDelete("{id}/data")]
    [EditorAuthorize]
    [SwaggerOperation(OperationId = "delete_job_id_data", Description = "Delete all job data", Summary = "Delete All Job Data")]
    [NoContentResponse]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<IActionResult> ClearData([FromRoute][Required] string id)
    {
        await BusinesLayer.ClearData(id);
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
    [CreatedResponse(typeof(PlanarIdResponse))]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<ActionResult<PlanarIdResponse>> QueueInvoke([FromBody] QueueInvokeJobRequest request)
    {
        var response = await BusinesLayer.QueueInvoke(request);
        return Created(string.Empty, response);
    }

    [HttpPost("pause")]
    [EditorAuthorize]
    [SwaggerOperation(OperationId = "post_job_pause", Description = "Pause job", Summary = "Pause Job")]
    [JsonConsumes]
    [AcceptedContentResponse]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<IActionResult> Pause([FromBody] PauseResumeJobRequest request)
    {
        await BusinesLayer.Pause(request);
        return Accepted();
    }

    [HttpPost("pause-group")]
    [EditorAuthorize]
    [SwaggerOperation(OperationId = "post_job_pause_group", Description = "Pause job group", Summary = "Pause Job Group")]
    [JsonConsumes]
    [AcceptedContentResponse]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<IActionResult> PauseGroup([FromBody] PauseResumeGroupRequest request)
    {
        await BusinesLayer.PauseGroup(request);
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

    [HttpPost("auto-resume")]
    [EditorAuthorize]
    [SwaggerOperation(OperationId = "post_job_auto_resume", Description = "Set job auto resume", Summary = "Set Job Auto Resume")]
    [JsonConsumes]
    [AcceptedContentResponse]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<IActionResult> SetAutoResume([FromBody] PauseResumeJobRequest request)
    {
        await BusinesLayer.SetAutoResume(request);
        return Accepted();
    }

    [HttpDelete("{id}/auto-resume")]
    [EditorAuthorize]
    [SwaggerOperation(OperationId = "delete_job_auto_resume", Description = "Delete job auto resume", Summary = "Delete Job Auto Resume")]
    [JsonConsumes]
    [AcceptedContentResponse]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<IActionResult> CancelAutoResume([FromRoute][Required] string id)
    {
        await BusinesLayer.CancelAutoResume(id);
        return Accepted();
    }

    [HttpPost("resume-group")]
    [EditorAuthorize]
    [SwaggerOperation(OperationId = "post_job_resume_group", Description = "Resume job group", Summary = "Resume Job Group")]
    [JsonConsumes]
    [AcceptedContentResponse]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<IActionResult> ResumeGroup([FromBody] PauseResumeGroupRequest request)
    {
        await BusinesLayer.ResumeGroup(request);
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
    [NotFoundResponse]
    [BadRequestResponse]
    public async Task<ActionResult<RunningJobDetails>> GetRunningInstance([FromRoute][Required] string instanceId)
    {
        instanceId = WebUtility.UrlDecode(instanceId);
        var result = await BusinesLayer.GetRunning(instanceId);
        return Ok(result);
    }

    [HttpGet("running-instance/{instanceId}/long-polling")]
    [ViewerAuthorize]
    [SwaggerOperation(OperationId = "get_job_running_instanceid_long_polling", Description = "Get runnng job info (Long polling)", Summary = "Get Runnng Job Info (Long Polling)")]
    [BadRequestResponse]
    [RequestTimeoutResponse]
    public async Task<ActionResult<RunningJobDetails>> GetRunningInstanceLongPollingV2(
        [FromRoute][Required] string instanceId,
        [FromQuery] int? progress,
        [FromQuery] int? effectedRows,
        [FromQuery] int? exceptionsCount,
        CancellationToken cancellationToken)
    {
        instanceId = WebUtility.UrlDecode(instanceId);
        var result = await BusinesLayer.GetRunningInstanceLongPolling(instanceId, progress, effectedRows, exceptionsCount, cancellationToken);
        return Ok(result);
    }

    ////[ApiExplorerSettings(IgnoreApi = true)]
    ////[HttpGet("running-instance/{instanceId}/long-polling")]
    ////[ViewerAuthorize]
    ////public async Task<ActionResult<RunningJobDetails>> GetRunningInstanceLongPollingV1(
    ////    [FromRoute][Required] string instanceId,
    ////    [FromQuery][Required] string hash,
    ////    CancellationToken cancellationToken)
    ////{
    ////    var result = await BusinesLayer.GetRunningInstanceLongPolling(instanceId, hash, cancellationToken);
    ////    return Ok(result);
    ////}

    [HttpGet("running")]
    [ViewerAuthorize]
    [SwaggerOperation(OperationId = "get_job_running", Description = "Gat all running jobs", Summary = "Gat All Running Jobs")]
    [OkJsonResponse(typeof(List<RunningJobDetails>))]
    public async Task<ActionResult<List<RunningJobDetails>>> GetAllRunning()
    {
        var result = await BusinesLayer.GetRunning();
        return Ok(result);
    }

    [HttpGet("running-data/{instanceId}")]
    [ViewerAuthorize]
    [SwaggerOperation(OperationId = "get_job_running_data_instanceid", Description = "Get running job log & exception", Summary = "Get Running Job Data")]
    [OkJsonResponse(typeof(RunningJobData))]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<ActionResult<RunningJobData>> GetRunningData([FromRoute][Required] string instanceId)
    {
        instanceId = WebUtility.UrlDecode(instanceId);
        var result = await BusinesLayer.GetRunningData(instanceId);
        return Ok(result);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("running-log/{instanceId}/sse")]
    [ViewerAuthorize]
    [SwaggerOperation(OperationId = "get_job_running_log_instanceid_sse", Description = "Get running job log", Summary = "Get running job log")]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task GetRunningLog([FromRoute][Required] string instanceId, CancellationToken cancellationToken)
    {
        instanceId = WebUtility.UrlDecode(instanceId);
        var serviceProvider = HttpContext.RequestServices;
        var sse = serviceProvider.GetRequiredService<JobDomainSse>();
        await sse.GetRunningLog(instanceId, cancellationToken);
    }

    [HttpGet("jobfile/{name}")]
    [EditorAuthorize]
    [SwaggerOperation(OperationId = "get_job_jobfile_name", Description = "Get JobFile.yml template", Summary = "Get JobFile.yml Template")]
    [OkYmlResponse]
    [BadRequestResponse]
    [NotFoundResponse]
    public ActionResult<string> GetJobFileTemplate([Required][FromRoute] string name)
    {
        name = WebUtility.UrlDecode(name);
        var result = JobDomain.GetJobFileTemplate(name);
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
    [HttpGet("{id}/last-instance-id/long-polling")]
    [TesterAuthorize]
    [OkJsonResponse(typeof(LastInstanceId))]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<ActionResult<LastInstanceId>> GetLastInstanceId(
        [FromRoute][Required] string id,
        [FromQuery][Required][SqlDateTime] DateTime invokeDate,
        CancellationToken cancellationToken)
    {
        var result = await BusinesLayer.GetLastInstanceId(id, invokeDate, cancellationToken);
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

    [HttpGet("wait")]
    [ViewerAuthorize]
    [SwaggerOperation(OperationId = "get_job_wait", Description = "Wait to finish running", Summary = "Wait To Finish Running")]
    [OkTextResponse]
    [BadRequestResponse]
    public async Task Wait([FromQuery] JobWaitRequest request, CancellationToken cancellationToken)
    {
        await BusinesLayer.Wait(request, cancellationToken);
    }

    [HttpPatch("author")]
    [EditorAuthorize]
    [SwaggerOperation(OperationId = "patch_job_author", Description = "Set the author of job", Summary = "Set The Author Of Job")]
    [JsonConsumes]
    [BadRequestResponse]
    [NoContentResponse]
    public async Task<IActionResult> SetAuthor([FromBody] SetJobAuthorRequest request)
    {
        await BusinesLayer.SetAuthor(request);
        return NoContent();
    }
}
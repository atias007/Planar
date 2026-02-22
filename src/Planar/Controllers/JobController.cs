using CloudNative.CloudEvents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using Planar.Validation.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Controllers;

[ApiController]
[Route("job")]
public class JobController(JobDomain bl) : BaseController<JobDomain>(bl)
{
    [HttpPost("failover-publish")]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult FailOverPublish([Required] CloudEvent request)
    {
        BusinesLayer.FailOverPublish(request);
        return NoContent();
    }

    [HttpPost("apply")]
    [EditorAuthorize]
    [EndpointName("post_job_apply")]
    [EndpointDescription("Add/Update job by yml file")]
    [EndpointSummary("Add/Update Job By Yml File")]
    [JsonAndYamlConsumes]
    [CreatedResponse(typeof(PlanarIdResponse))]
    [BadRequestResponse]
    public async Task<ActionResult<PlanarIdResponse>> Apply()
    {
        var result = await BusinesLayer.ApplyRoute(HttpContext);
        return CreatedAtAction(nameof(Get), result, result);
    }

    [HttpPost]
    [EditorAuthorize]
    [EndpointName("post_job")]
    [EndpointDescription("Add job by yml file")]
    [EndpointSummary("Add Job By Yml File")]
    [JsonAndYamlConsumes]
    [CreatedResponse(typeof(PlanarIdResponse))]
    [BadRequestResponse]
    [ConflictResponse]
    public async Task<ActionResult<PlanarIdResponse>> Add()
    {
        var result = await BusinesLayer.AddRoute(HttpContext);
        return CreatedAtAction(nameof(Get), result, result);
    }

    [HttpPut]
    [EditorAuthorize]
    [EndpointName("put_job_id")]
    [EndpointDescription("Update job")]
    [EndpointSummary("Update Job")]
    [JsonAndYamlConsumes]
    [CreatedResponse(typeof(PlanarIdResponse))]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<ActionResult<PlanarIdResponse>> Update()
    {
        var result = await BusinesLayer.UpdateRoute(HttpContext);
        return CreatedAtAction(nameof(Get), result, result);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("available-jobs")]
    [EditorAuthorize]
    [EndpointName("get_job_available_jobs_mode")]
    [EndpointDescription("")]
    [EndpointSummary("")]
    [OkJsonResponse(typeof(List<AvailableJob>))]
    public async Task<ActionResult<List<AvailableJob>>> GetAvailableJobs([FromQuery] bool update)
    {
        var result = await BusinesLayer.GetAvailableJobs(update);
        return Ok(result);
    }

    [HttpGet]
    [ViewerAuthorize]
    [BadRequestResponse]
    [EndpointName("get_job")]
    [EndpointDescription("Get all jobs")]
    [EndpointSummary("Get All Jobs")]
    [OkJsonResponse(typeof(PagingResponse<JobBasicDetails>))]
    public async Task<ActionResult<PagingResponse<JobBasicDetails>>> GetAll([FromQuery] GetAllJobsRequest request)
    {
        var result = await BusinesLayer.GetAll(request);
        return Ok(result);
    }

    [HttpGet("ids")]
    [AllowAnonymous]
    [EndpointName("get_job_ids")]
    [EndpointDescription("Get all jobs ids")]
    [EndpointSummary("Get All Jobs Ids")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [OkJsonResponse(typeof(IEnumerable<string>))]
    public async Task<ActionResult<IEnumerable<string>>> GetAllIds()
    {
        var result = await BusinesLayer.GetAllIds();
        return Ok(result);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("jobfilename/{id}")]
    [ViewerAuthorize]
    [BadRequestResponse]
    [NotFoundResponse]
    [EndpointName("get_job_file_id")]
    [EndpointDescription("Get JobFile.yml filename")]
    [EndpointSummary("Get JobFile.yml Filename")]
    [OkTextResponse]
    public async Task<ActionResult<string>> GetJobFilename([FromRoute][Required] string id)
    {
        var result = await BusinesLayer.GetJobFilename(id);
        return Ok(result);
    }

    [HttpGet("groups")]
    [ViewerAuthorize]
    [EndpointName("get_groups")]
    [EndpointDescription("Get job groups")]
    [EndpointSummary("Get Job Groups")]
    [OkJsonResponse(typeof(IEnumerable<string>))]
    public async Task<ActionResult<IEnumerable<string>>> GetGroupNames()
    {
        var result = await BusinesLayer.GetJobGroupNames();
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [EditorAuthorize]
    [EndpointName("delete_job_id")]
    [EndpointDescription("Delete job")]
    [EndpointSummary("Delete Job")]
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
    [EndpointName("get_job_id")]
    [EndpointDescription("Get job details by id")]
    [EndpointSummary("Get Job By Id")]
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
    [EndpointName("get_job_info_id")]
    [EndpointDescription("Get job info by id")]
    [EndpointSummary("Get Job Info By Id")]
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
    [EndpointName("get_job_next_running_id")]
    [EndpointDescription("Get the next running date & time of job")]
    [EndpointSummary("Get Next Running Date")]
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
    [EndpointName("get_job_prev_running_id")]
    [EndpointDescription("Get the previous running date & time of job")]
    [EndpointSummary("Get Previous Running Date")]
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
    [EndpointName("post_job_data")]
    [EndpointDescription("Add job data")]
    [EndpointSummary("Add Job Data")]
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
    [EndpointName("put_job_data")]
    [EndpointDescription("Update job data")]
    [EndpointSummary("Update Job Data")]
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
    [EndpointName("delete_job_id_data_key")]
    [EndpointDescription("Delete job data")]
    [EndpointSummary("Delete Job Data")]
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
    [EndpointName("delete_job_id_data")]
    [EndpointDescription("Delete all job data")]
    [EndpointSummary("Delete All Job Data")]
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
    [EndpointName("post_job_invoke")]
    [EndpointDescription("Invoke job")]
    [EndpointSummary("Invoke Job")]
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
    [EndpointName("post_job_queue_invoke")]
    [EndpointDescription("Queue invokation of job")]
    [EndpointSummary("Queue Invokation Of Job")]
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
    [EndpointName("post_job_pause")]
    [EndpointDescription("Pause job")]
    [EndpointSummary("Pause Job")]
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
    [EndpointName("post_job_pause_group")]
    [EndpointDescription("Pause job group")]
    [EndpointSummary("Pause Job Group")]
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
    [EndpointName("post_job_resume")]
    [EndpointDescription("Resume job")]
    [EndpointSummary("Resume Job")]
    [JsonConsumes]
    [AcceptedContentResponse]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<IActionResult> Resume([FromBody] PauseResumeJobRequest request)
    {
        await BusinesLayer.Resume(request);
        return Accepted();
    }

    [HttpPost("auto-resume")]
    [EditorAuthorize]
    [EndpointName("post_job_auto_resume")]
    [EndpointDescription("Set job auto resume")]
    [EndpointSummary("Set Job Auto Resume")]
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
    [EndpointName("delete_job_auto_resume")]
    [EndpointDescription("Delete job auto resume")]
    [EndpointSummary("Delete Job Auto Resume")]
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
    [EndpointName("post_job_resume_group")]
    [EndpointDescription("Resume job group")]
    [EndpointSummary("Resume Job Group")]
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
    [EndpointName("post_job_cancel")]
    [EndpointDescription("Cancel running job")]
    [EndpointSummary("Cancel Job")]
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
    [EndpointName("gat_job_id_settings")]
    [EndpointDescription("Get job settings")]
    [EndpointSummary("Get Job Settings")]
    [OkJsonResponse(typeof(IEnumerable<KeyValueItem>))]
    [BadRequestResponse]
    public async Task<ActionResult<IEnumerable<KeyValueItem>>> GetSettings([FromRoute][Required] string id)
    {
        var result = await BusinesLayer.GetSettings(id);
        return Ok(result);
    }

    [HttpGet("running-instance/{instanceId}")]
    [ViewerAuthorize]
    [EndpointName("get_job_running_instanceid")]
    [EndpointDescription("Get runnng job info")]
    [EndpointSummary("Get Runnng Job Info")]
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
    [EndpointName("get_job_running_instanceid_long_polling")]
    [EndpointDescription("Get runnng job info (Long polling)")]
    [EndpointSummary("Get Runnng Job Info (Long Polling)")]
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
    [EndpointName("get_job_running")]
    [EndpointDescription("Gat all running jobs")]
    [EndpointSummary("Gat All Running Jobs")]
    [OkJsonResponse(typeof(List<RunningJobDetails>))]
    public async Task<ActionResult<List<RunningJobDetails>>> GetAllRunning()
    {
        var result = await BusinesLayer.GetRunning();
        return Ok(result);
    }

    [HttpGet("running-data/{instanceId}")]
    [ViewerAuthorize]
    [EndpointName("get_job_running_data_instanceid")]
    [EndpointDescription("Get running job log & exception")]
    [EndpointSummary("Get Running Job Data")]
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
    [EndpointName("get_job_running_log_instanceid_sse")]
    [EndpointDescription("Get running job log")]
    [EndpointSummary("Get running job log")]
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
    [EndpointName("get_job_jobfile_name")]
    [EndpointDescription("Get JobFile.yml template")]
    [EndpointSummary("Get JobFile.yml Template")]
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
    [EndpointName("get_job_types")]
    [EndpointDescription("Get all job types")]
    [EndpointSummary("Get All Job Types")]
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
    [EndpointName("get_job_id_audit")]
    [EndpointDescription("Get audits for job")]
    [EndpointSummary("Get Audits For Job")]
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
    [EndpointName("get_job_audit_audit_id")]
    [EndpointDescription("Get audit by id")]
    [EndpointSummary("Get Audit By Id")]
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
    [EndpointName("get_job_audits")]
    [EndpointDescription("Get all audits")]
    [EndpointSummary("Get All Audits")]
    [BadRequestResponse]
    [OkJsonResponse(typeof(PagingResponse<JobAuditDto>))]
    public async Task<ActionResult<PagingResponse<JobAuditDto>>> GetJobAudits([FromQuery] PagingRequest request)
    {
        var result = await BusinesLayer.GetAudits(request);
        return Ok(result);
    }

    [HttpGet("wait")]
    [ViewerAuthorize]
    [EndpointName("get_job_wait")]
    [EndpointDescription("Wait to finish running")]
    [EndpointSummary("Wait To Finish Running")]
    [OkTextResponse]
    [BadRequestResponse]
    public async Task Wait([FromQuery] JobWaitRequest request, CancellationToken cancellationToken)
    {
        await BusinesLayer.Wait(request, cancellationToken);
    }

    [HttpPatch("author")]
    [EditorAuthorize]
    [EndpointName("patch_job_author")]
    [EndpointDescription("Set the author of job")]
    [EndpointSummary("Set The Author Of Job")]
    [JsonConsumes]
    [BadRequestResponse]
    [NoContentResponse]
    public async Task<IActionResult> SetAuthor([FromBody] SetJobAuthorRequest request)
    {
        await BusinesLayer.SetAuthor(request);
        return NoContent();
    }
}
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Planar.Controllers;

[ApiController]
[Route("metrics")]
public class MetricsController(MetricsDomain businesLayer) : BaseController<MetricsDomain>(businesLayer)
{
    [HttpPost("rebuild")]
    [EditorAuthorize]
    [EndpointName("post_metrics_rebuild")]
    [EndpointDescription("Rebuild statistics data")]
    [EndpointSummary("Rebuild Statistics Data")]
    [NoContentResponse]
    public async Task<ActionResult> RebuildJobStatistics()
    {
        await BusinesLayer.RebuildJobStatistics();
        return NoContent();
    }

    [HttpGet("job/{id}")]
    [ViewerAuthorize]
    [EndpointName("get_metrics_job_id")]
    [EndpointDescription("Get job metrics")]
    [EndpointSummary("Get Job Metrics")]
    [OkJsonResponse(typeof(JobMetrics))]
    [BadRequestResponse]
    public async Task<ActionResult<JobMetrics>> GetJobMetrics([FromRoute][Required] string id)
    {
        var response = await BusinesLayer.GetJobMetrics(id);
        return Ok(response);
    }

    [HttpGet("concurrent")]
    [ViewerAuthorize]
    [EndpointName("get_metrics_concurrent")]
    [EndpointDescription("Get concurrent execution")]
    [EndpointSummary("Get Concurrent Execution")]
    [OkJsonResponse(typeof(IEnumerable<ConcurrentExecutionModel>))]
    [BadRequestResponse]
    public async Task<ActionResult<PagingResponse<ConcurrentExecutionModel>>> GetConcurrentExecution([FromQuery] ConcurrentExecutionRequest request)
    {
        var response = await BusinesLayer.GetConcurrentExecution(request);
        return Ok(response);
    }

    [HttpGet("max-concurrent")]
    [ViewerAuthorize]
    [EndpointName("get_metrics_max_concurrent")]
    [EndpointDescription("Get max of concurrent execution")]
    [EndpointSummary("Get Max Of Concurrent Execution")]
    [OkJsonResponse(typeof(MaxConcurrentExecution))]
    [BadRequestResponse]
    public async Task<ActionResult<MaxConcurrentExecution>> GetMaxConcurrentExecution([FromQuery] MaxConcurrentExecutionRequest request)
    {
        var response = await BusinesLayer.GetMaxConcurrentExecution(request);
        return Ok(response);
    }

    [HttpGet("job-counters")]
    [ViewerAuthorize]
    [EndpointName("get_metrics_job_counters")]
    [EndpointDescription("Get all jobs counters")]
    [EndpointSummary("Get All Jobs Counters")]
    [OkJsonResponse(typeof(JobCounters))]
    [BadRequestResponse]
    public async Task<ActionResult<JobCounters>> GetAllJobsCounters([FromQuery] AllJobsCountersRequest request)
    {
        var response = await BusinesLayer.GetAllJobsCounters(request);
        return Ok(response);
    }
}
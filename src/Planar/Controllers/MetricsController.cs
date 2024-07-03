using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using Swashbuckle.AspNetCore.Annotations;
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
    [SwaggerOperation(OperationId = "post_metrics_rebuild", Description = "Rebuild statistics data", Summary = "Rebuild Statistics Data")]
    [NoContentResponse]
    public async Task<ActionResult> RebuildJobStatistics()
    {
        await BusinesLayer.RebuildJobStatistics();
        return NoContent();
    }

    [HttpGet("job/{id}")]
    [ViewerAuthorize]
    [SwaggerOperation(OperationId = "get_metrics_job_id", Description = "Get job metrics", Summary = "Get Job Metrics")]
    [OkJsonResponse(typeof(JobMetrics))]
    [BadRequestResponse]
    public async Task<ActionResult<JobMetrics>> GetJobMetrics([FromRoute][Required] string id)
    {
        var response = await BusinesLayer.GetJobMetrics(id);
        return Ok(response);
    }

    [HttpGet("concurrent")]
    [ViewerAuthorize]
    [SwaggerOperation(OperationId = "get_metrics_concurrent", Description = "Get concurrent execution", Summary = "Get Concurrent Execution")]
    [OkJsonResponse(typeof(IEnumerable<ConcurrentExecutionModel>))]
    [BadRequestResponse]
    public async Task<ActionResult<PagingResponse<ConcurrentExecutionModel>>> GetConcurrentExecution([FromQuery] ConcurrentExecutionRequest request)
    {
        var response = await BusinesLayer.GetConcurrentExecution(request);
        return Ok(response);
    }

    [HttpGet("max-concurrent")]
    [ViewerAuthorize]
    [SwaggerOperation(OperationId = "get_metrics_max_concurrent", Description = "Get max of concurrent execution", Summary = "Get Max Of Concurrent Execution")]
    [OkJsonResponse(typeof(MaxConcurrentExecution))]
    [BadRequestResponse]
    public async Task<ActionResult<MaxConcurrentExecution>> GetMaxConcurrentExecution([FromQuery] MaxConcurrentExecutionRequest request)
    {
        var response = await BusinesLayer.GetMaxConcurrentExecution(request);
        return Ok(response);
    }

    [HttpGet("job-counters")]
    [ViewerAuthorize]
    [SwaggerOperation(OperationId = "get_metrics_job_counters", Description = "Get all jobs counters", Summary = "Get All Jobs Counters")]
    [OkJsonResponse(typeof(JobCounters))]
    [BadRequestResponse]
    public async Task<ActionResult<JobCounters>> GetAllJobsCounters([FromQuery] AllJobsCountersRequest request)
    {
        var response = await BusinesLayer.GetAllJobsCounters(request);
        return Ok(response);
    }
}
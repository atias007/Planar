using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using Planar.Service.Model;
using Planar.Validation.Attributes;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Planar.Controllers;

[ApiController]
[Route("history")]
public class HistoryController(HistoryDomain bl) : BaseController<HistoryDomain>(bl)
{
    [HttpGet]
    [ViewerAuthorize]
    [SwaggerOperation(OperationId = "get_history", Description = "Get history data by filter", Summary = "Get History")]
    [OkJsonResponse(typeof(PagingResponse<JobInstanceLogRow>))]
    [BadRequestResponse]
    public async Task<ActionResult<PagingResponse<JobInstanceLogRow>>> GetHistory([FromQuery] GetHistoryRequest request)
    {
        var result = await BusinesLayer.GetHistory(request);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [ViewerAuthorize]
    [SwaggerOperation(OperationId = "get_history_id", Description = "Get history by id", Summary = "Get History By Id")]
    [OkJsonResponse(typeof(JobInstanceLog))]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<ActionResult<JobInstanceLog>> GetHistoryById([FromRoute][LongId] long id)
    {
        var result = await BusinesLayer.GetHistoryById(id);
        return Ok(result);
    }

    [HttpGet("by-instanceid/{instanceid}")]
    [ViewerAuthorize]
    [SwaggerOperation(OperationId = "get_history_by_instanceid_instanceid", Description = "Get history by instance id", Summary = "Get History By Instance Id")]
    [OkJsonResponse(typeof(JobInstanceLog))]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<ActionResult<JobInstanceLog>> GetHistoryByInstanceId([FromRoute][Required] string instanceid)
    {
        instanceid = WebUtility.UrlDecode(instanceid);
        var result = await BusinesLayer.GetHistoryByInstanceId(instanceid);
        return Ok(result);
    }

    [HttpGet("{id}/data")]
    [ViewerAuthorize]
    [SwaggerOperation(OperationId = "get_history_id_data", Description = "Get only variables data from specific history item", Summary = "Get History Data By Id")]
    [OkTextResponse]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<IActionResult> GetHistoryDataById([FromRoute][LongId] long id)
    {
        var result = await BusinesLayer.GetHistoryDataById(id);
        return new FileStreamResult(result, MediaTypeNames.Text.Plain)
        {
            FileDownloadName = $"planar_history_data_{id}.log"
        };
    }

    [HttpGet("by-instanceid/{instanceid}/data")]
    [ViewerAuthorize]
    [SwaggerOperation(OperationId = "get_history_by-instanceid_instanceid_data", Description = "Get only variables data from specific history item", Summary = "Get History Data By Instance Id")]
    [OkTextResponse]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<IActionResult> GetHistoryDataByInstanceId([FromRoute][Required] string instanceid)
    {
        var result = await BusinesLayer.GetHistoryDataByInstanceId(instanceid);
        return new FileStreamResult(result, MediaTypeNames.Text.Plain)
        {
            FileDownloadName = $"planar_history_data_{instanceid}.log"
        };
    }

    [HttpGet("{id}/log")]
    [ViewerAuthorize]
    [SwaggerOperation(OperationId = "get_history_id_log", Description = "Get only log text from specific history item", Summary = "Get History Log By Id")]
    [OkTextResponse]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<IActionResult> GetHistoryLogById([FromRoute][LongId] long id)
    {
        var result = await BusinesLayer.GetHistoryLogById(id);
        return new FileStreamResult(result, MediaTypeNames.Text.Plain)
        {
            FileDownloadName = $"planar_history_log_{id}.log"
        };
    }

    [HttpGet("by-instanceid/{instanceid}/log")]
    [ViewerAuthorize]
    [SwaggerOperation(OperationId = "get_history_by-instanceid_instanceid_log", Description = "Get only log text from specific history item", Summary = "Get History Log By Instance Id")]
    [OkTextResponse]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<IActionResult> GetHistoryLogByInstanceId([FromRoute][Required] string instanceid)
    {
        var result = await BusinesLayer.GetHistoryLogByInstanceId(instanceid);
        return new FileStreamResult(result, MediaTypeNames.Text.Plain)
        {
            FileDownloadName = $"planar_history_log_{instanceid}.log"
        };
    }

    [HttpGet("{id}/exception")]
    [ViewerAuthorize]
    [SwaggerOperation(OperationId = "get_history_id_exception", Description = "Get only exceptions text from specific history item", Summary = "Get History Exceptions By Id")]
    [OkTextResponse]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<IActionResult> GetHistoryExceptionById([FromRoute][LongId] long id)
    {
        var result = await BusinesLayer.GetHistoryExceptionById(id);
        return new FileStreamResult(result, MediaTypeNames.Text.Plain)
        {
            FileDownloadName = $"planar_history_exception_{id}.log"
        };
    }

    [HttpGet("by-instanceid/{instanceid}/exception")]
    [ViewerAuthorize]
    [SwaggerOperation(OperationId = "get_history_by-instanceid_instanceid_exception", Description = "Get only exceptions text from specific history item", Summary = "Get History Exceptions By Instance Id")]
    [OkTextResponse]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<IActionResult> GetHistoryExceptionByInstanceId([FromRoute][Required] string instanceid)
    {
        var result = await BusinesLayer.GetHistoryExceptionByInstanceId(instanceid);
        return new FileStreamResult(result, MediaTypeNames.Text.Plain)
        {
            FileDownloadName = $"planar_history_exception_{instanceid}.log"
        };
    }

    [HttpGet("last")]
    [ViewerAuthorize]
    [SwaggerOperation(OperationId = "get_history_last", Description = "Get last running of each job", Summary = "Get Last Running Of Each Job")]
    [OkJsonResponse(typeof(PagingResponse<JobHistory>))]
    [BadRequestResponse]
    public async Task<ActionResult<PagingResponse<JobLastRun>>> GetLastHistoryCallForJob([FromQuery] GetLastHistoryCallForJobRequest request)
    {
        var result = await BusinesLayer.GetLastHistoryCallForJob(request);
        return Ok(result);
    }

    [HttpGet("summary")]
    [ViewerAuthorize]
    [SwaggerOperation(OperationId = "get_history_summary", Description = "Get summary of last running jobs", Summary = "Get Summary Of Last Running Jobs")]
    [OkJsonResponse(typeof(PagingResponse<HistorySummary>))]
    [BadRequestResponse]
    public async Task<ActionResult<PagingResponse<HistorySummary>>> GetHistorySummary([FromQuery] GetSummaryRequest request)
    {
        var result = await BusinesLayer.GetHistorySummary(request);
        return Ok(result);
    }

    [HttpGet("count")]
    [ViewerAuthorize]
    [SwaggerOperation(OperationId = "get_history_count", Description = "Get history count by status", Summary = "Get History Count")]
    [OkJsonResponse(typeof(CounterResponse))]
    [BadRequestResponse]
    public async Task<ActionResult<CounterResponse>> GetHistoryCounter([FromQuery] CounterRequest request)
    {
        var result = await BusinesLayer.GetHistoryCounter(request);
        return Ok(result);
    }

    [HttpGet("{id}/status")]
    [ViewerAuthorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<int>> GetHistoryStatusById([FromRoute][LongId] long id)
    {
        var result = await BusinesLayer.GetHistoryStatusById(id);
        return Ok(result);
    }
}
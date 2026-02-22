using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using Planar.Service.Model;
using Planar.Validation.Attributes;
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
    [EndpointName("get_history")]
    [EndpointDescription("Get history data by filter")]
    [EndpointSummary("Get History")]
    [OkJsonResponse(typeof(PagingResponse<JobInstanceLogRow>))]
    [BadRequestResponse]
    public async Task<ActionResult<PagingResponse<JobInstanceLogRow>>> GetHistory([FromQuery] GetHistoryRequest request)
    {
        var result = await BusinesLayer.GetHistory(request);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [ViewerAuthorize]
    [EndpointName("get_history_id")]
    [EndpointDescription("Get history by id")]
    [EndpointSummary("Get History By Id")]
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
    [EndpointName("get_history_by_instanceid_instanceid")]
    [EndpointDescription("Get history by instance id")]
    [EndpointSummary("Get History By Instance Id")]
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
    [EndpointName("get_history_id_data")]
    [EndpointDescription("Get only variables data from specific history item")]
    [EndpointSummary("Get History Data By Id")]
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
    [EndpointName("get_history_by-instanceid_instanceid_data")]
    [EndpointDescription("Get only variables data from specific history item")]
    [EndpointSummary("Get History Data By Instance Id")]
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
    [EndpointName("get_history_id_log")]
    [EndpointDescription("Get only log text from specific history item")]
    [EndpointSummary("Get History Log By Id")]
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
    [EndpointName("get_history_by-instanceid_instanceid_log")]
    [EndpointDescription("Get only log text from specific history item")]
    [EndpointSummary("Get History Log By Instance Id")]
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
    [EndpointName("get_history_id_exception")]
    [EndpointDescription("Get only exceptions text from specific history item")]
    [EndpointSummary("Get History Exceptions By Id")]
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
    [EndpointName("get_history_by-instanceid_instanceid_exception")]
    [EndpointDescription("Get only exceptions text from specific history item")]
    [EndpointSummary("Get History Exceptions By Instance Id")]
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
    [EndpointName("get_history_last")]
    [EndpointDescription("Get last running of each job")]
    [EndpointSummary("Get Last Running Of Each Job")]
    [OkJsonResponse(typeof(PagingResponse<JobHistory>))]
    [BadRequestResponse]
    public async Task<ActionResult<PagingResponse<JobLastRun>>> GetLastHistoryCallForJob([FromQuery] GetLastHistoryCallForJobRequest request)
    {
        var result = await BusinesLayer.GetLastHistoryCallForJob(request);
        return Ok(result);
    }

    [HttpGet("summary")]
    [ViewerAuthorize]
    [EndpointName("get_history_summary")]
    [EndpointDescription("Get summary of last running jobs")]
    [EndpointSummary("Get Summary Of Last Running Jobs")]
    [OkJsonResponse(typeof(PagingResponse<HistorySummary>))]
    [BadRequestResponse]
    public async Task<ActionResult<PagingResponse<HistorySummary>>> GetHistorySummary([FromQuery] GetSummaryRequest request)
    {
        var result = await BusinesLayer.GetHistorySummary(request);
        return Ok(result);
    }

    [HttpGet("count")]
    [ViewerAuthorize]
    [EndpointName("get_history_count")]
    [EndpointDescription("Get history count by status")]
    [EndpointSummary("Get History Count")]
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
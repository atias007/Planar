using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using Planar.Service.Model;
using Planar.Validation.Attributes;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [ApiController]
    [Route("history")]
    public class HistoryController : BaseController<HistoryDomain>
    {
        public HistoryController(HistoryDomain bl) : base(bl)
        {
        }

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
            var result = await BusinesLayer.GetHistoryByInstanceId(instanceid);
            return Ok(result);
        }

        [HttpGet("{id}/data")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_history_id_data", Description = "Get only variables data from specific history item", Summary = "Get History Data By Id")]
        [OkTextResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<string>> GetHistoryDataById([FromRoute][LongId] long id)
        {
            var result = await BusinesLayer.GetHistoryDataById(id);
            return Ok(result);
        }

        [HttpGet("{id}/log")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_history_id_log", Description = "Get only log text from specific history item", Summary = "Get History Log By Id")]
        [OkTextResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<string>> GetHistoryLogById([FromRoute][LongId] long id)
        {
            var result = await BusinesLayer.GetHistoryLogById(id);
            return Ok(result);
        }

        [HttpGet("{id}/exception")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_history_id_exception", Description = "Get only exceptions text from specific history item", Summary = "Get History Exceptions By Id")]
        [OkTextResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<string>> GetHistoryExceptionById([FromRoute][LongId] long id)
        {
            var result = await BusinesLayer.GetHistoryExceptionById(id);
            return Ok(result);
        }

        [HttpGet("last")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_history_last", Description = "Get summary of last running of each job", Summary = "Get Last Running Per Job")]
        [OkJsonResponse(typeof(PagingResponse<JobHistory>))]
        [BadRequestResponse]
        public async Task<ActionResult<PagingResponse<JobHistory>>> GetLastHistoryCallForJob([FromQuery] GetLastHistoryCallForJobRequest request)
        {
            var result = await BusinesLayer.GetLastHistoryCallForJob(request);
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
    }
}
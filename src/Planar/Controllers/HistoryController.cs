using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Service.API;
using Planar.Service.Model;
using Planar.Validation.Attributes;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [Route("history")]
    public class HistoryController : BaseController<HistoryDomain>
    {
        public HistoryController(HistoryDomain bl) : base(bl)
        {
        }

        [HttpGet]
        [OkJsonResponse(typeof(List<JobInstanceLog>))]
        public async Task<ActionResult<List<JobInstanceLog>>> GetHistory([FromQuery] GetHistoryRequest request)
        {
            var result = await BusinesLayer.GetHistory(request);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [OkJsonResponse(typeof(JobInstanceLog))]
        public async Task<ActionResult<JobInstanceLog>> GetHistoryById([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetHistoryById(id);
            return Ok(result);
        }

        [HttpGet("{id}/data")]
        [OkTextResponse]
        public async Task<ActionResult<string>> GetHistoryDataById([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetHistoryDataById(id);
            return Ok(result);
        }

        [HttpGet("{id}/log")]
        [OkTextResponse]
        public async Task<ActionResult<string>> GetHistoryLogById([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetHistoryLogById(id);
            return Ok(result);
        }

        [HttpGet("{id}/exception")]
        [OkTextResponse]
        public async Task<ActionResult<string>> GetHistoryExceptionById([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetHistoryExceptionById(id);
            return Ok(result);
        }

        [HttpGet("last")]
        [OkJsonResponse(typeof(JobInstanceLog))]
        public async Task<ActionResult<List<JobInstanceLog>>> GetLastHistoryCallForJob([FromQuery][UInt] int lastDays)
        {
            var result = await BusinesLayer.GetLastHistoryCallForJob(lastDays);
            return Ok(result);
        }
    }
}
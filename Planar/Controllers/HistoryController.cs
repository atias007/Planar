using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.API;
using Planar.Validation.Attributes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [ApiController]
    [Route("history")]
    public class HistoryController : BaseController<HistoryController, HistoryDomain>
    {
        public HistoryController(ILogger<HistoryController> logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        [HttpGet]
        public async Task<ActionResult<List<JobInstanceLogRow>>> GetHistory([FromQuery] GetHistoryRequest request)
        {
            var result = await BusinesLayer.GetHistory(request);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<JobInstanceLog>> GetHistoryById([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetHistoryById(id);
            return Ok(result);
        }

        [HttpGet("{id}/data")]
        public async Task<ActionResult<string>> GetHistoryDataById([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetHistoryDataById(id);
            return Ok(result);
        }

        [HttpGet("{id}/log")]
        public async Task<ActionResult<string>> GetHistoryLogById([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetHistoryLogById(id);
            return Ok(result);
        }

        [HttpGet("{id}/exception")]
        public async Task<ActionResult<string>> GetHistoryExceptionById([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetHistoryExceptionById(id);
            return Ok(result);
        }

        [HttpGet("last")]
        public async Task<ActionResult<List<JobInstanceLogRow>>> GetLastHistoryCallForJob([FromQuery][UInt] int lastDays)
        {
            var result = await BusinesLayer.GetLastHistoryCallForJob(lastDays);
            return Ok(result);
        }
    }
}
﻿using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [Route("statistics")]
    public class StatisticsController : BaseController<StatisticsDomain>
    {
        public StatisticsController(StatisticsDomain businesLayer) : base(businesLayer)
        {
        }

        [HttpGet("job/{id}")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_statistics_job_id", Description = "Get job statistics", Summary = "Get Job Statistics")]
        [OkJsonResponse(typeof(JobStatistic))]
        [BadRequestResponse]
        public async Task<ActionResult<JobStatistic>> GetJobStatistics([FromRoute][Required] string id)
        {
            var response = await BusinesLayer.GetJobStatistics(id);
            return Ok(response);
        }

        [HttpGet("concurrent")]
        [ViewerAuthorize]
        [SwaggerOperation(OperationId = "get_statistics_concurrent", Description = "Get concurrent execution statistics", Summary = "Get Concurrent Execution Statistics")]
        [OkJsonResponse(typeof(IEnumerable<ConcurrentExecutionModel>))]
        [BadRequestResponse]
        public async Task<ActionResult<IEnumerable<ConcurrentExecutionModel>>> GetConcurrentExecution([FromQuery] ConcurrentExecutionRequest request)
        {
            var response = await BusinesLayer.GetConcurrentExecution(request);
            return Ok(response);
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Planar.Api.Common.Entities;
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
    [ApiController]
    [Route("report")]
    public class ReportController : BaseController<ReportDomain>
    {
        public ReportController(ReportDomain businesLayer) : base(businesLayer)
        {
        }

        [HttpPatch("summary")]
        [EditorAuthorize]
        [JsonConsumes]
        [SwaggerOperation(OperationId = "patch_report_summary", Description = "Update summary report definition", Summary = "Update Summary Report Definition")]
        [NoContentResponse]
        [BadRequestResponse]
        public async Task<IActionResult> Update([FromBody] UpdateReportRequest request)
        {
            await BusinesLayer.Update(request);
            return NoContent();
        }

        [HttpGet("{name}")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_report_name", Description = "Get report schedule details", Summary = "Get Report Schedule Details")]
        [BadRequestResponse]
        [NotFoundResponse]
        [OkJsonResponse(typeof(IEnumerable<ReportsStatus>))]
        public async Task<ActionResult<IEnumerable<ReportsStatus>>> Get([FromRoute][Required] string name)
        {
            var result = await BusinesLayer.GetReport(name);
            return Ok(result);
        }

        [HttpGet]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_report", Description = "Get list of reports", Summary = "Get List Of Reports")]
        [OkJsonResponse(typeof(IEnumerable<string>))]
        public ActionResult<IEnumerable<string>> GetAll()
        {
            var result = ReportDomain.GetReports();
            return Ok(result);
        }

        [HttpGet("periods")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_report_periods", Description = "Get list of report periods", Summary = "Get List Of Reports Periods")]
        [OkJsonResponse(typeof(IEnumerable<string>))]
        public ActionResult<IEnumerable<string>> GetAllPeriods()
        {
            var result = ReportDomain.GetPeriods();
            return Ok(result);
        }
    }
}
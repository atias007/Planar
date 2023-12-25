using Microsoft.AspNetCore.Mvc;
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

        [HttpPatch("{name}")]
        [EditorAuthorize]
        [JsonConsumes]
        [SwaggerOperation(OperationId = "patch_report_name", Description = "Update report definition", Summary = "Update Report Definition")]
        [NoContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<IActionResult> Update([FromRoute][Required] string name, [FromBody] UpdateReportRequest request)
        {
            await BusinesLayer.Update(name, request);
            return NoContent();
        }

        [HttpGet("{name}")]
        [TesterAuthorize]
        [SwaggerOperation(OperationId = "get_report_name", Description = "Get report schedule details", Summary = "Get Report Schedule Details")]
        [BadRequestResponse]
        [NotFoundResponse]
        [OkJsonResponse(typeof(IEnumerable<ReportsStatus>))]
        public async Task<ActionResult<IEnumerable<ReportsStatus>>> Get([FromRoute][Required] string name)
        {
            var result = await BusinesLayer.GetReport(name);
            return Ok(result);
        }

        [HttpPost("{name}/run")]
        [TesterAuthorize]
        [JsonConsumes]
        [SwaggerOperation(OperationId = "post_report_name_run", Description = "Run and deliver report", Summary = "Run And Deliver Report")]
        [BadRequestResponse]
        [NotFoundResponse]
        [AcceptedContentResponse]
        public async Task<IActionResult> Run([FromRoute][Required] string name, [FromBody] RunReportRequest request)
        {
            await BusinesLayer.Run(name, request);
            return Accepted();
        }

        [HttpGet]
        [TesterAuthorize]
        [SwaggerOperation(OperationId = "get_report", Description = "Get list of reports", Summary = "Get List Of Reports")]
        [OkJsonResponse(typeof(IEnumerable<string>))]
        public ActionResult<IEnumerable<string>> GetAll()
        {
            var result = ReportDomain.GetReports();
            return Ok(result);
        }

        [HttpGet("periods")]
        [TesterAuthorize]
        [SwaggerOperation(OperationId = "get_report_periods", Description = "Get list of report periods", Summary = "Get List Of Reports Periods")]
        [OkJsonResponse(typeof(IEnumerable<string>))]
        public ActionResult<IEnumerable<string>> GetAllPeriods()
        {
            var result = ReportDomain.GetPeriods();
            return Ok(result);
        }
    }
}
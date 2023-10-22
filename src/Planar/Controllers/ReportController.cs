using Microsoft.AspNetCore.Mvc;
using Planar.Api.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using Swashbuckle.AspNetCore.Annotations;
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
        public async Task<IActionResult> Update([FromBody] UpdateSummaryReportRequest request)
        {
            await BusinesLayer.Update(request);
            return NoContent();
        }
    }
}
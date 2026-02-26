using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;

namespace Planar.Controllers;

[ApiController]
[Route("report")]
public class ReportController(ReportDomain businesLayer) : BaseController<ReportDomain>(businesLayer)
{
    [HttpPatch("{name}")]
    [EditorAuthorize]
    [JsonConsumes]
    [EndpointName("patch_report_name")]
    [EndpointDescription("Update report definition")]
    [EndpointSummary("Update Report Definition")]
    [NoContentResponse]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<IActionResult> Update([FromRoute][Required] string name, [FromBody] UpdateReportRequest request)
    {
        name = WebUtility.UrlDecode(name);
        await BusinesLayer.Update(name, request);
        return NoContent();
    }

    [HttpGet("{name}")]
    [TesterAuthorize]
    [EndpointName("get_report_name")]
    [EndpointDescription("Get report schedule details")]
    [EndpointSummary("Get Report Schedule Details")]
    [BadRequestResponse]
    [NotFoundResponse]
    [OkJsonResponse(typeof(IEnumerable<ReportsStatus>))]
    public async Task<ActionResult<IEnumerable<ReportsStatus>>> Get([FromRoute][Required] string name)
    {
        name = WebUtility.UrlDecode(name);
        var result = await BusinesLayer.GetReport(name);
        return Ok(result);
    }

    [HttpPost("{name}/run")]
    [TesterAuthorize]
    [JsonConsumes]
    [EndpointName("post_report_name_run")]
    [EndpointDescription("Run and deliver report")]
    [EndpointSummary("Run And Deliver Report")]
    [BadRequestResponse]
    [NotFoundResponse]
    [AcceptedContentResponse]
    public async Task<IActionResult> Run([FromRoute][Required] string name, [FromBody] RunReportRequest request)
    {
        name = WebUtility.UrlDecode(name);
        await BusinesLayer.Run(name, request);
        return Accepted();
    }

    [HttpGet]
    [TesterAuthorize]
    [EndpointName("get_report")]
    [EndpointDescription("Get list of reports")]
    [EndpointSummary("Get List Of Reports")]
    [OkJsonResponse(typeof(IEnumerable<string>))]
    public ActionResult<IEnumerable<string>> GetAll()
    {
        var result = ReportDomain.GetReports();
        return Ok(result);
    }

    [HttpGet("periods")]
    [TesterAuthorize]
    [EndpointName("get_report_periods")]
    [EndpointDescription("Get list of report periods")]
    [EndpointSummary("Get List Of Reports Periods")]
    [OkJsonResponse(typeof(IEnumerable<string>))]
    public ActionResult<IEnumerable<string>> GetAllPeriods()
    {
        var result = ReportDomain.GetPeriods();
        return Ok(result);
    }
}
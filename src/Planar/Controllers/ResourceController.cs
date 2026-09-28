using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using Planar.Service.Model;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Planar.Controllers;

[ApiController]
[Route("resource")]
public class ResourceController(ResourceDomain bl) : BaseController<ResourceDomain>(bl)
{
    [HttpPost("apply")]
    [EditorAuthorize]
    [EndpointName("post_resource_apply")]
    [EndpointDescription("Add/Update resource")]
    [EndpointSummary("Add/Update Resource")]
    [YamlConsumes]
    [OkJsonResponse(typeof(ApplyResponse))]
    [MultiStatusJsonResponse(typeof(ApplyResponse))]
    [BadRequestResponse]
    public async Task<ActionResult<ApplyResponse>> Apply()
    {
        var result = await BusinesLayer.Apply(HttpContext);
        var status = result.GetStatusCode();
        return StatusCode((int)status, result);
    }

    [HttpPost]
    [EditorAuthorize]
    [JsonConsumes]
    [EndpointName("post_resource")]
    [EndpointDescription("Add resource")]
    [EndpointSummary("Add Resource")]
    [CreatedResponse]
    [BadRequestResponse]
    [ConflictResponse]
    public async Task<IActionResult> Add([FromBody] ResourceModel request)
    {
        await BusinesLayer.Add(request);
        return CreatedAtAction(nameof(GetByName), new { request.Name }, null);
    }

    [HttpPut]
    [EditorAuthorize]
    [JsonConsumes]
    [EndpointName("put_resource")]
    [EndpointDescription("Update resource")]
    [EndpointSummary("Update Resource")]
    [NoContentResponse]
    [NotFoundResponse]
    [BadRequestResponse]
    public async Task<IActionResult> Update([FromBody] ResourceModel request)
    {
        await BusinesLayer.Update(request);
        return NoContent();
    }

    [HttpGet]
    [EditorAuthorize]
    [EndpointName("get_resource")]
    [EndpointDescription("Get all resources")]
    [EndpointSummary("Get All Resources")]
    [OkJsonResponse(typeof(PagingResponse<ResourceModel>))]
    [BadRequestResponse]
    public async Task<ActionResult<PagingResponse<ResourceModel>>> GetAll([FromQuery] PagingRequest request)
    {
        var result = await BusinesLayer.GetAll(request);
        return Ok(result);
    }

    [HttpGet("{name}")]
    [EditorAuthorize]
    [EndpointName("get_resource_name")]
    [EndpointDescription("Get resource by name")]
    [EndpointSummary("Get Resource By Name")]
    [OkJsonResponse(typeof(ResourceModel))]
    public async Task<ActionResult<ResourceModel>> GetByName([FromRoute][Length(1, 100)] string name)
    {
        var result = await BusinesLayer.GetByName(name);
        return Ok(result);
    }

    [HttpDelete("{name}")]
    [EditorAuthorize]
    [EndpointName("delete_resource")]
    [EndpointDescription("Delete resource")]
    [EndpointSummary("Delete Resource")]
    [NoContentResponse]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<ActionResult> Delete([FromRoute][Length(1, 100)] string name)
    {
        await BusinesLayer.Delete(name);
        return NoContent();
    }
}
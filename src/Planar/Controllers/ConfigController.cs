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
[Route("config")]
public class ConfigController(ConfigDomain bl) : BaseController<ConfigDomain>(bl)
{
    [HttpGet]
    [EditorAuthorize]
    [EndpointName("get_config")]
    [EndpointDescription("Get all global configuration")]
    [EndpointSummary("Get All Global Configurations")]
    [OkJsonResponse(typeof(IEnumerable<GlobalConfigModel>))]
    public async Task<ActionResult<IEnumerable<GlobalConfigModel>>> GetAll()
    {
        var result = await BusinesLayer.GetAll();
        return Ok(result);
    }

    [HttpGet("flat")]
    [EditorAuthorize]
    [EndpointName("get_config_flat")]
    [EndpointDescription("Get all global configuration")]
    [EndpointSummary("Get All Global Configurations")]
    [OkJsonResponse(typeof(IEnumerable<KeyValueItem>))]
    public async Task<ActionResult<IEnumerable<KeyValueItem>>> GetAllFlat()
    {
        var result = await Task.FromResult(ConfigDomain.GetAllFlat());
        return Ok(result);
    }

    [HttpGet("{key}")]
    [EditorAuthorize]
    [EndpointName("get_config_key")]
    [EndpointDescription("Get global configuration by key")]
    [EndpointSummary("Get Global Configuration")]
    [OkJsonResponse(typeof(GlobalConfigModel))]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<ActionResult<GlobalConfigModel>> Get([FromRoute][Required] string key)
    {
        key = WebUtility.UrlDecode(key);
        var result = await BusinesLayer.Get(key);
        return Ok(result);
    }

    [HttpPost]
    [EditorAuthorize]
    [EndpointName("post_config")]
    [EndpointDescription("Add new global configuration")]
    [EndpointSummary("Add Global Configuration")]
    [JsonConsumes]
    [CreatedResponse]
    [BadRequestResponse]
    [ConflictResponse]
    public async Task<ActionResult> Add([FromBody] GlobalConfigModelAddRequest request)
    {
        await BusinesLayer.Add(request);
        return CreatedAtAction(nameof(Get), new { key = request.Key }, null);
    }

    [HttpPut]
    [EditorAuthorize]
    [EndpointName("put_config")]
    [EndpointDescription("Update existing global configuration")]
    [EndpointSummary("Update Global Configuration")]
    [JsonConsumes]
    [NoContentResponse]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<ActionResult> Update([FromBody] GlobalConfigModelAddRequest request)
    {
        await BusinesLayer.Update(request);
        return NoContent();
    }

    [HttpDelete("{key}")]
    [AdministratorAuthorize]
    [EndpointName("delete_config_key")]
    [EndpointDescription("Delete existing global configuration")]
    [EndpointSummary("Delete Global Configuration")]
    [NoContentResponse]
    [NotFoundResponse]
    public async Task<ActionResult> Delete([FromRoute] string key)
    {
        key = WebUtility.UrlDecode(key);
        await BusinesLayer.Delete(key);
        return NoContent();
    }

    [HttpPost("flush")]
    [TesterAuthorize]
    [EndpointName("post_config_flush")]
    [EndpointDescription("Flush and reload global configuration from cache")]
    [EndpointSummary("Flush All Global Configuration")]
    [NoContentResponse]
    public async Task<ActionResult> Flush()
    {
        await BusinesLayer.FlushWithReloadExternalSourceUrl();
        return NoContent();
    }
}
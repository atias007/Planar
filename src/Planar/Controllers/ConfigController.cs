using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using Planar.Service.Model;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [ApiController]
    [Route("config")]
    public class ConfigController : BaseController<ConfigDomain>
    {
        public ConfigController(ConfigDomain bl) : base(bl)
        {
        }

        [HttpGet]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_config", Description = "Get all global configuration", Summary = "Get All Global Configurations")]
        [OkJsonResponse(typeof(IEnumerable<GlobalConfig>))]
        public async Task<ActionResult<IEnumerable<GlobalConfig>>> GetAll()
        {
            var result = await BusinesLayer.GetAll();
            return Ok(result);
        }

        [HttpGet("flat")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_config_flat", Description = "Get all global configuration", Summary = "Get All Global Configurations")]
        [OkJsonResponse(typeof(IEnumerable<KeyValueItem>))]
        public async Task<ActionResult<IEnumerable<KeyValueItem>>> GetAllFlat()
        {
            var result = await Task.FromResult(BusinesLayer.GetAllFlat());
            return Ok(result);
        }

        [HttpGet("{key}")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_config_key", Description = "Get global configuration by key", Summary = "Get Global Configuration")]
        [OkJsonResponse(typeof(GlobalConfig))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<GlobalConfig>> Get([FromRoute][Required] string key)
        {
            var result = await BusinesLayer.Get(key);
            return Ok(result);
        }

        [HttpPost]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "post_config", Description = "Add new global configuration", Summary = "Add Global Configuration")]
        [JsonConsumes]
        [CreatedResponse]
        [BadRequestResponse]
        [ConflictResponse]
        public async Task<ActionResult> Add([FromBody] GlobalConfig request)
        {
            await BusinesLayer.Add(request);
            return CreatedAtAction(nameof(Get), new { key = request.Key }, null);
        }

        [HttpPut]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "put_config", Description = "Update existing global configuration", Summary = "Update Global Configuration")]
        [JsonConsumes]
        [NoContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult> Update([FromBody] GlobalConfig request)
        {
            await BusinesLayer.Update(request);
            return NoContent();
        }

        [HttpDelete("{key}")]
        [AdministratorAuthorize]
        [SwaggerOperation(OperationId = "delete_config_key", Description = "Delete existing global configuration", Summary = "Delete Global Configuration")]
        [NoContentResponse]
        [NotFoundResponse]
        public async Task<ActionResult> Delete([FromRoute] string key)
        {
            await BusinesLayer.Delete(key);
            return NoContent();
        }

        [HttpPost("flush")]
        [TesterAuthorize]
        [SwaggerOperation(OperationId = "post_config_flush", Description = "Flush and reload global configuration from cache", Summary = "Flush All Global Configuration")]
        [NoContentResponse]
        public async Task<ActionResult> Flush()
        {
            await BusinesLayer.Flush();
            return NoContent();
        }
    }
}
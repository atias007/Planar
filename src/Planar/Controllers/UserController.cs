using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using Planar.Service.Model;
using Planar.Validation.Attributes;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [Route("user")]
    public class UserController : BaseController<UserDomain>
    {
        public UserController(UserDomain bl) : base(bl)
        {
        }

        [HttpPost]
        [EditorAuthorize]
        [JsonConsumes]
        [SwaggerOperation(OperationId = "post_user", Description = "Add user", Summary = "Add User")]
        [CreatedResponse(typeof(AddUserResponse))]
        [BadRequestResponse]
        [ConflictResponse]
        public async Task<ActionResult<AddUserResponse>> Add([FromBody] AddUserRequest request)
        {
            var result = await BusinesLayer.Add(request);
            return CreatedAtAction(nameof(Get), new { result.Id }, result);
        }

        [HttpPut]
        [EditorAuthorize]
        [JsonConsumes]
        [SwaggerOperation(OperationId = "put_user", Description = "Update user", Summary = "Update User")]
        [NoContentResponse]
        [NotFoundResponse]
        [ConflictResponse]
        [BadRequestResponse]
        public async Task<IActionResult> Update([FromBody] UpdateUserRequest request)
        {
            await BusinesLayer.Update(request);
            return NoContent();
        }

        [HttpGet("{id}")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_user_id", Description = "Get user by id", Summary = "Get User")]
        [OkJsonResponse(typeof(UserDetails))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<UserDetails>> Get([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.Get(id);
            return Ok(result);
        }

        [HttpGet("{id}/role")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_user_id_role", Description = "Get user rule by id", Summary = "Get User Role")]
        [OkTextResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<UserDetails>> GetRole([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetRole(id);
            return Ok(result);
        }

        [HttpGet]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_user", Description = "Get all users", Summary = "Get All Users")]
        [OkJsonResponse(typeof(List<UserRow>))]
        public async Task<ActionResult<List<UserRow>>> GetAll()
        {
            var result = await BusinesLayer.GetAll();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [AdministratorAuthorize]
        [SwaggerOperation(OperationId = "delete_user_id", Description = "Delete user", Summary = "Delete User")]
        [NoContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult> Delete([FromRoute][Id] int id)
        {
            await BusinesLayer.Delete(id);
            return NoContent();
        }

        [HttpPatch]
        [EditorAuthorize]
        [JsonConsumes]
        [SwaggerOperation(OperationId = "patch_user", Description = "Update user single property", Summary = "Partial Update Group")]
        [NoContentResponse]
        [NotFoundResponse]
        [ConflictResponse]
        [BadRequestResponse]
        public async Task<ActionResult> PartialUpdate([FromBody] UpdateEntityRequest request)
        {
            await BusinesLayer.PartialUpdate(request);
            return NoContent();
        }

        [HttpPatch("{id}/resetpassword")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "patch_user_id_resetpassword", Description = "Reset user password", Summary = "Reset User Password")]
        [OkTextResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<string>> ResetPassword([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.ResetPassword(id);
            return Ok(result);
        }
    }
}
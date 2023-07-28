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
    [ApiController]
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
            return CreatedAtAction(nameof(Get), new { request.Username }, result);
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

        [HttpGet("{username}")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_user_username", Description = "Get user by username", Summary = "Get User")]
        [OkJsonResponse(typeof(UserDetails))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<UserDetails>> Get([FromRoute][Name] string username)
        {
            var result = await BusinesLayer.Get(username);
            return Ok(result);
        }

        [HttpGet("{username}/role")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_user_username_role", Description = "Get user rule by username", Summary = "Get User Role")]
        [OkTextResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<UserDetails>> GetRole([FromRoute][Name] string username)
        {
            var result = await BusinesLayer.GetRole(username);
            return Ok(result);
        }

        [HttpGet]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_user", Description = "Get all users", Summary = "Get All Users")]
        [OkJsonResponse(typeof(PagingResponse<UserRow>))]
        public async Task<ActionResult<PagingResponse<UserRow>>> GetAll([FromQuery] PagingRequest request)
        {
            var result = await BusinesLayer.GetAll(request);
            return Ok(result);
        }

        [HttpDelete("{username}")]
        [AdministratorAuthorize]
        [SwaggerOperation(OperationId = "delete_user_username", Description = "Delete user", Summary = "Delete User")]
        [NoContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult> Delete([FromRoute][Name] string username)
        {
            await BusinesLayer.Delete(username);
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
        public async Task<ActionResult> PartialUpdate([FromBody] UpdateEntityRequestByName request)
        {
            await BusinesLayer.PartialUpdate(request);
            return NoContent();
        }

        [HttpPatch("{username}/reset-password")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "patch_user_username_reset_password", Description = "Reset user password", Summary = "Reset User Password")]
        [OkTextResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<string>> ResetPassword([FromRoute][Name] string username)
        {
            var result = await BusinesLayer.ResetPassword(username);
            return Ok(result);
        }

        [HttpPatch("{username}/password")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "patch_user_username_password", Description = "Set user password", Summary = "Set User Password")]
        [NoContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<IActionResult> SetPassword([FromRoute][Name] string username, [FromBody] SetPasswordRequest request)
        {
            await BusinesLayer.SetPassword(username, request);
            return NoContent();
        }
    }
}
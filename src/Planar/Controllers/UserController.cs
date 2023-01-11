using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Attributes;
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
        [SwaggerOperation(OperationId = "get_user_id", Description = "Get user by id", Summary = "Get User")]
        [OkJsonResponse(typeof(UserDetails))]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<ActionResult<UserDetails>> Get([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.Get(id);
            return Ok(result);
        }

        [HttpGet]
        [SwaggerOperation(OperationId = "get_user", Description = "Get all users", Summary = "Get All Users")]
        [OkJsonResponse(typeof(List<UserRow>))]
        public async Task<ActionResult<List<UserRow>>> GetAll()
        {
            var result = await BusinesLayer.GetAll();
            return Ok(result);
        }

        [HttpDelete("{id}")]
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
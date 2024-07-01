using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using Planar.Validation.Attributes;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [ApiController]
    [Route("group")]
    public class GroupController(GroupDomain bl) : BaseController<GroupDomain>(bl)
    {
        [HttpPost]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "post_group", Description = "Add new group", Summary = "Add Group")]
        [CreatedResponse]
        [JsonConsumes]
        [ConflictResponse]
        [ForbiddenResponse]
        public async Task<ActionResult<string>> AddGroup([FromBody] AddGroupRequest request)
        {
            await BusinesLayer.AddGroup(request);
            return CreatedAtAction(nameof(GetGroup), new { request.Name }, null);
        }

        [HttpGet("{name}")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_group_name", Description = "Get group by name", Summary = "Get Group")]
        [OkJsonResponse(typeof(GroupDetails))]
        [NotFoundResponse]
        public async Task<ActionResult<GroupDetails>> GetGroup([FromRoute][Name] string name)
        {
            name = WebUtility.UrlDecode(name);
            var result = await BusinesLayer.GetGroupByName(name);
            return Ok(result);
        }

        [HttpGet]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_group", Description = "Get all groups", Summary = "Get All Groups")]
        [OkJsonResponse(typeof(PagingResponse<GroupInfo>))]
        public async Task<ActionResult<PagingResponse<GroupInfo>>> GetAllGroups([FromQuery] PagingRequest request)
        {
            var result = await BusinesLayer.GetAllGroups(request);
            return Ok(result);
        }

        [HttpGet("roles")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "get_group_roles", Description = "Get all group roles", Summary = "Get All Group Roles")]
        [OkJsonResponse(typeof(IEnumerable<string>))]
        public async Task<ActionResult<IEnumerable<string>>> GetAllGroupsRoles()
        {
            var result = await Task.FromResult(GroupDomain.GetAllGroupsRoles());
            return Ok(result);
        }

        [HttpDelete("{name}")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "delete_group_name", Description = "Delete group", Summary = "Delete Group")]
        [NoContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<IActionResult> DeleteGroup([FromRoute][Name] string name)
        {
            name = WebUtility.UrlDecode(name);
            await BusinesLayer.DeleteGroup(name);
            return NoContent();
        }

        [HttpPatch]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "patch_group", Description = "Update group single property", Summary = "Partial Update Group")]
        [JsonConsumes]
        [NoContentResponse]
        [BadRequestResponse]
        [ConflictResponse]
        [NotFoundResponse]
        public async Task<IActionResult> PatrialUpdateGroup([FromBody] UpdateEntityRequestByName request)
        {
            await BusinesLayer.PartialUpdateGroup(request);
            return NoContent();
        }

        [HttpPut]
        [EditorAuthorize]
        [JsonConsumes]
        [SwaggerOperation(OperationId = "put_group", Description = "Update group", Summary = "Update Group")]
        [NoContentResponse]
        [BadRequestResponse]
        [ConflictResponse]
        [NotFoundResponse]
        public async Task<IActionResult> Update([FromBody] UpdateGroupRequest request)
        {
            await BusinesLayer.Update(request);
            return NoContent();
        }

        [HttpPatch("{name}/user/{username}")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "post_group_name_user_userId", Description = "Join user to group", Summary = "Join User To Group")]
        [NoContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        [ForbiddenResponse]
        public async Task<IActionResult> AddUserToGroup([FromRoute][Name] string name, [FromRoute][Name] string username)
        {
            name = WebUtility.UrlDecode(name);
            await BusinesLayer.AddUserToGroup(name, username);
            return NoContent();
        }

        [HttpPatch("{name}/role/{role}")]
        [AdministratorAuthorize]
        [SwaggerOperation(OperationId = "patch_group_name_role_role", Description = "Set role to group", Summary = "Set Role To Group")]
        [NoContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        [ForbiddenResponse]
        public async Task<IActionResult> SetRoleToGroup([FromRoute][Name] string name, [FromRoute][Required][MaxLength(20)] string role)
        {
            name = WebUtility.UrlDecode(name);
            await BusinesLayer.SetRoleToGroup(name, role);
            return NoContent();
        }

        [HttpDelete("{name}/user/{username}")]
        [EditorAuthorize]
        [SwaggerOperation(OperationId = "delete_group_id_user", Description = "Remove user from group", Summary = "Remove User From Group")]
        [NoContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<IActionResult> RemoveUserFromGroup([FromRoute][Name] string name, [FromRoute][Name] string username)
        {
            name = WebUtility.UrlDecode(name);
            username = WebUtility.UrlDecode(username);
            await BusinesLayer.RemoveUserFromGroup(name, username);
            return NoContent();
        }
    }
}
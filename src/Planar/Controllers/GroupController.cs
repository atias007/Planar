using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using Planar.Validation.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;

namespace Planar.Controllers;

[ApiController]
[Route("group")]
public class GroupController(GroupDomain bl) : BaseController<GroupDomain>(bl)
{
    [HttpPost]
    [EditorAuthorize]
    [EndpointName("post_group")]
    [EndpointDescription("Add new group")]
    [EndpointSummary("Add Group")]
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
    [EndpointName("get_group_name")]
    [EndpointDescription("Get group by name")]
    [EndpointSummary("Get Group")]
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
    [EndpointName("get_group")]
    [EndpointDescription("Get all groups")]
    [EndpointSummary("Get All Groups")]
    [OkJsonResponse(typeof(PagingResponse<GroupInfo>))]
    public async Task<ActionResult<PagingResponse<GroupInfo>>> GetAllGroups([FromQuery] PagingRequest request)
    {
        var result = await BusinesLayer.GetAllGroups(request);
        return Ok(result);
    }

    [HttpGet("roles")]
    [EditorAuthorize]
    [EndpointName("get_group_roles")]
    [EndpointDescription("Get all group roles")]
    [EndpointSummary("Get All Group Roles")]
    [OkJsonResponse(typeof(IEnumerable<string>))]
    public async Task<ActionResult<IEnumerable<string>>> GetAllGroupsRoles()
    {
        var result = await Task.FromResult(GroupDomain.GetAllGroupsRoles());
        return Ok(result);
    }

    [HttpDelete("{name}")]
    [EditorAuthorize]
    [EndpointName("delete_group_name")]
    [EndpointDescription("Delete group")]
    [EndpointSummary("Delete Group")]
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
    [EndpointName("patch_group")]
    [EndpointDescription("Update group single property")]
    [EndpointSummary("Partial Update Group")]
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
    [EndpointName("put_group")]
    [EndpointDescription("Update group")]
    [EndpointSummary("Update Group")]
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
    [EndpointName("post_group_name_user_userId")]
    [EndpointDescription("Join user to group")]
    [EndpointSummary("Join User To Group")]
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
    [EndpointName("patch_group_name_role_role")]
    [EndpointDescription("Set role to group")]
    [EndpointSummary("Set Role To Group")]
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
    [EndpointName("delete_group_id_user")]
    [EndpointDescription("Remove user from group")]
    [EndpointSummary("Remove User From Group")]
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
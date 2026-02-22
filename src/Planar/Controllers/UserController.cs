using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using Planar.Service.Model;
using Planar.Validation.Attributes;
using System.Net;
using System.Threading.Tasks;

namespace Planar.Controllers;

[ApiController]
[Route("user")]
public class UserController(UserDomain bl) : BaseController<UserDomain>(bl)
{
    [HttpPost]
    [EditorAuthorize]
    [JsonConsumes]
    [EndpointName("post_user")]
    [EndpointDescription("Add user")]
    [EndpointSummary("Add User")]
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
    [EndpointName("put_user")]
    [EndpointDescription("Update user")]
    [EndpointSummary("Update User")]
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
    [EndpointName("get_user_username")]
    [EndpointDescription("Get user by username")]
    [EndpointSummary("Get User")]
    [OkJsonResponse(typeof(UserDetails))]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<ActionResult<UserDetails>> Get([FromRoute][Name] string username)
    {
        username = WebUtility.UrlDecode(username);
        var result = await BusinesLayer.Get(username);
        return Ok(result);
    }

    [HttpGet("{username}/role")]
    [EditorAuthorize]
    [EndpointName("get_user_username_role")]
    [EndpointDescription("Get user rule by username")]
    [EndpointSummary("Get User Role")]
    [OkTextResponse]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<ActionResult<string>> GetRole([FromRoute][Name] string username)
    {
        username = WebUtility.UrlDecode(username);
        var result = await BusinesLayer.GetRole(username);
        return Ok(result);
    }

    [HttpGet]
    [EditorAuthorize]
    [EndpointName("get_user")]
    [EndpointDescription("Get all users")]
    [EndpointSummary("Get All Users")]
    [OkJsonResponse(typeof(PagingResponse<UserRowModel>))]
    public async Task<ActionResult<PagingResponse<UserRowModel>>> GetAll([FromQuery] PagingRequest request)
    {
        var result = await BusinesLayer.GetAll(request);
        return Ok(result);
    }

    [HttpDelete("{username}")]
    [AdministratorAuthorize]
    [EndpointName("delete_user_username")]
    [EndpointDescription("Delete user")]
    [EndpointSummary("Delete User")]
    [NoContentResponse]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<ActionResult> Delete([FromRoute][Name] string username)
    {
        username = WebUtility.UrlDecode(username);
        await BusinesLayer.Delete(username);
        return NoContent();
    }

    [HttpPatch]
    [EditorAuthorize]
    [JsonConsumes]
    [EndpointName("patch_user")]
    [EndpointDescription("Update user single property")]
    [EndpointSummary("Partial Update Group")]
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
    [EndpointName("patch_user_username_reset_password")]
    [EndpointDescription("Reset user password")]
    [EndpointSummary("Reset User Password")]
    [OkTextResponse]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<ActionResult<string>> ResetPassword([FromRoute][Name] string username)
    {
        username = WebUtility.UrlDecode(username);
        var result = await BusinesLayer.ResetPassword(username);
        return Ok(result);
    }

    [HttpPatch("{username}/password")]
    [EditorAuthorize]
    [EndpointName("patch_user_username_password")]
    [EndpointDescription("Set user password")]
    [EndpointSummary("Set User Password")]
    [NoContentResponse]
    [BadRequestResponse]
    [NotFoundResponse]
    public async Task<IActionResult> SetPassword([FromRoute][Name] string username, [FromBody] SetPasswordRequest request)
    {
        username = WebUtility.UrlDecode(username);
        await BusinesLayer.SetPassword(username, request);
        return NoContent();
    }
}
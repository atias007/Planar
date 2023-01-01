using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Service.API;
using Planar.Validation.Attributes;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [Route("group")]
    public class GroupController : BaseController<GroupDomain>
    {
        public GroupController(GroupDomain bl) : base(bl)
        {
        }

        [HttpPost]
        [SwaggerOperation(OperationId = "post_group", Description = "Add new group", Summary = "Add Group")]
        [CreatedResponse(typeof(EntityIdResponse))]
        [JsonConsumes]
        [ConflictResponse]
        public async Task<ActionResult<EntityIdResponse>> AddGroup([FromBody] AddGroupRequest request)
        {
            var id = await BusinesLayer.AddGroup(request);
            return CreatedAtAction(nameof(GetGroup), id, id);
        }

        [HttpGet("{id}")]
        [SwaggerOperation(OperationId = "get_group_id", Description = "Get group by id", Summary = "Get Group")]
        [OkJsonResponse(typeof(GroupDetails))]
        [NotFoundResponse]
        public async Task<ActionResult<GroupDetails>> GetGroup([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.GetGroupById(id);
            return Ok(result);
        }

        [HttpGet]
        [SwaggerOperation(OperationId = "get_group", Description = "Get all groups", Summary = "Get All Groups")]
        [OkJsonResponse(typeof(List<GroupInfo>))]
        public async Task<ActionResult<List<GroupInfo>>> GetAllGroups()
        {
            var result = await BusinesLayer.GetAllGroups();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(OperationId = "delete_group_id", Description = "Delete group by id", Summary = "Delete Group")]
        [NoContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<IActionResult> DeleteGroup([FromRoute][Id] int id)
        {
            await BusinesLayer.DeleteGroup(id);
            return NoContent();
        }

        [HttpPatch]
        [SwaggerOperation(OperationId = "patch_group", Description = "Update group single property", Summary = "Partial Update Group")]
        [JsonConsumes]
        [NoContentResponse]
        [BadRequestResponse]
        [ConflictResponse]
        [NotFoundResponse]
        public async Task<IActionResult> PatrialUpdateGroup([FromBody] UpdateEntityRecord request)
        {
            await BusinesLayer.PartialUpdateGroup(request);
            return NoContent();
        }

        [HttpPut]
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

        [HttpPut("{id}/user/{userId}")]
        [SwaggerOperation(OperationId = "post_group_id_user", Description = "Add user to group", Summary = "Add User To Group")]
        [NoContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<IActionResult> AddUserToGroup([FromRoute][Id] int id, [FromRoute][Id] int userId)
        {
            await BusinesLayer.AddUserToGroup(id, userId);
            return NoContent();
        }

        [HttpDelete("{id}/user/{userId}")]
        [SwaggerOperation(OperationId = "delete_group_id_user", Description = "Remove user from group", Summary = "Remove User From Group")]
        [NoContentResponse]
        [BadRequestResponse]
        [NotFoundResponse]
        public async Task<IActionResult> RemoveUserFromGroup([FromRoute][Id] int id, [FromRoute][Id] int userId)
        {
            await BusinesLayer.RemoveUserFromGroup(id, userId);
            return NoContent();
        }
    }
}
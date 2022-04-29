using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Planar.Service.API;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [ApiController]
    [Route("group")]
    public class GroupController : BaseController<GroupController, GroupServiceDomain>
    {
        public GroupController(ILogger<GroupController> logger, GroupServiceDomain bl) : base(logger, bl)
        {
        }

        [HttpPost]
        public async Task<IActionResult> AddGroup(UpsertGroupRecord request)
        {
            var id = await BusinesLayer.AddGroup(request);
            return CreatedAtAction("GetGroup", new { id }, id);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetGroup([FromRoute] int id)
        {
            var result = await BusinesLayer.GetGroupById(id);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetGroups()
        {
            var result = await BusinesLayer.GetGroups();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGroup([FromRoute] int id)
        {
            await BusinesLayer.DeleteGroup(id);
            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateGroup([FromRoute] int id, [FromBody] UpdateEntityRecord request)
        {
            await BusinesLayer.UpdateGroup(id, request);
            return NoContent();
        }

        [HttpPost("{id}/user")]
        public async Task<IActionResult> AddUserToGroup([FromRoute] int id, [FromBody] UserToGroupRecord request)
        {
            await BusinesLayer.AddUserToGroup(id, request);
            return CreatedAtAction("GetGroup", new { id }, id);
        }

        [HttpDelete("{id}/user/{userId}")]
        public async Task<IActionResult> RemoveUserFromGroup([FromRoute] int id, [FromRoute] int userId)
        {
            await BusinesLayer.RemoveUserFromGroup(id, userId);
            return NoContent();
        }
    }
}
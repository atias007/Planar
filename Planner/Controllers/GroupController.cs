using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Planner.Service.API;
using System.Threading.Tasks;

namespace Planner.Controllers
{
    [ApiController]
    [Route("group")]
    public class GroupController : BaseController
    {
        private readonly ServiceDomain _bl;

        public GroupController(ILogger<GroupController> logger, ServiceDomain bl) : base(logger)
        {
            _bl = bl;
        }

        [HttpPost]
        public async Task<IActionResult> AddGroup(UpsertGroupRecord request)
        {
            var id = await _bl.AddGroup(request);
            return CreatedAtAction("GetGroup", new { id }, id);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetGroup([FromRoute] int id)
        {
            var result = await _bl.GetGroupById(id);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetGroups()
        {
            var result = await _bl.GetGroups();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGroup([FromRoute] int id)
        {
            await _bl.DeleteGroup(id);
            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateGroup([FromRoute] int id, [FromBody] UpdateEntityRecord request)
        {
            await _bl.UpdateGroup(id, request);
            return NoContent();
        }

        [HttpPost("{id}/user")]
        public async Task<IActionResult> AddUserToGroup([FromRoute] int id, [FromBody] UserToGroupRecord request)
        {
            await _bl.AddUserToGroup(id, request);
            return CreatedAtAction("GetGroup", new { id }, id);
        }

        [HttpDelete("{id}/user/{userId}")]
        public async Task<IActionResult> RemoveUserFromGroup([FromRoute] int id, [FromRoute] int userId)
        {
            await _bl.RemoveUserFromGroup(id, userId);
            return NoContent();
        }
    }
}
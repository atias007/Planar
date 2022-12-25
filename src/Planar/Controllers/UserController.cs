using Microsoft.AspNetCore.Mvc;
using Planar.API.Common.Entities;
using Planar.Attributes;
using Planar.Service.API;
using Planar.Service.Model;
using Planar.Validation.Attributes;
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
        public async Task<ActionResult<AddUserResponse>> Add([FromBody] AddUserRequest request)
        {
            var result = await BusinesLayer.Add(request);
            return CreatedAtAction(nameof(Get), new { result.Id }, result);
        }

        [HttpPut]
        [JsonConsumes]
        public async Task<IActionResult> Update([FromBody] UpdateUserRequest request)
        {
            await BusinesLayer.Update(request);
            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> Get([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.Get(id);
            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult<List<UserRow>>> GetAll()
        {
            var result = await BusinesLayer.GetAll();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete([FromRoute][Id] int id)
        {
            await BusinesLayer.Delete(id);
            return NoContent();
        }

        [HttpPatch("{id}")]
        [JsonConsumes]
        public async Task<ActionResult> PartialUpdate([FromRoute][Id] int id, [FromBody] UpdateEntityRecord request)
        {
            await BusinesLayer.PartialUpdate(id, request);
            return NoContent();
        }

        [HttpPatch("{id}/resetpassword")]
        public async Task<ActionResult<string>> ResetPassword([FromRoute][Id] int id)
        {
            var result = await BusinesLayer.ResetPassword(id);
            return Ok(result);
        }
    }
}
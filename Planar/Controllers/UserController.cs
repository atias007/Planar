using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.API;
using Planar.Service.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [ApiController]
    [Route("user")]
    public class UserController : BaseController<UserController, UserDomain>
    {
        public UserController(ILogger<UserController> logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        [HttpPost]
        public async Task<ActionResult<AddUserResponse>> Add([FromBody] AddUserRequest request)
        {
            var result = await BusinesLayer.Add(request);
            return CreatedAtAction(nameof(Get), new { result.Id }, result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> Get([FromRoute] int id)
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
        public async Task<ActionResult> Delete([FromRoute] int id)
        {
            await BusinesLayer.Delete(id);
            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> Update([FromRoute] int id, [FromBody] UpdateEntityRecord request)
        {
            await BusinesLayer.Update(id, request);
            return NoContent();
        }

        [HttpGet("{id}/password")]
        public async Task<ActionResult<string>> GetPassword([FromRoute] int id)
        {
            var result = await BusinesLayer.GetPassword(id);
            return Ok(result);
        }
    }
}
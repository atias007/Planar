using Microsoft.AspNetCore.Mvc;
using Planar.Service.API;
using Planar.Service.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [ApiController]
    [Route("cluster")]
    public class ClusterController : BaseController<ClusterDomain>
    {
        public ClusterController(ClusterDomain bl) : base(bl)
        {
        }

        [HttpGet("nodes")]
        public async Task<ActionResult<List<ClusterNode>>> GetNodes()
        {
            var response = await BusinesLayer.GetNodes();
            return Ok(response);
        }
    }
}
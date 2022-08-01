using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Planar.Service.API;
using Planar.Service.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [ApiController]
    [Route("cluster")]
    public class ClusterController : BaseController<ClusterController, ClusterDomain>
    {
        public ClusterController(ILogger<ClusterController> logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
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
using Microsoft.AspNetCore.Mvc;
using Planar.Attributes;
using Planar.Service.API;
using Planar.Service.Model;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Controllers
{
    [Route("cluster")]
    public class ClusterController : BaseController<ClusterDomain>
    {
        public ClusterController(ClusterDomain bl) : base(bl)
        {
        }

        [HttpGet("nodes")]
        [SwaggerOperation(OperationId = "get_cluster_nodes", Description = "Get list of all nodes in cluster", Summary = "Get Cluster Nodes")]
        [OkJsonResponse(typeof(List<ClusterNode>))]
        public async Task<ActionResult<List<ClusterNode>>> GetNodes()
        {
            var response = await BusinesLayer.GetNodes();
            return Ok(response);
        }

        [HttpGet("healthCheck")]
        [SwaggerOperation(OperationId = "get_cluster_health-check", Description = "Check the health of all nodes in cluster", Summary = "Check Cluster Health")]
        [OkTextResponse]
        [ServiceUnavailableResponse]
        public async Task<ActionResult<string>> HealthCheck()
        {
            var response = await BusinesLayer.HealthCheck();
            return Ok(response);
        }
    }
}
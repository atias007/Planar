using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Planar.Attributes;
using Planar.Authorization;
using Planar.Service.API;
using Planar.Service.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Controllers;

[ApiController]
[Route("cluster")]
public class ClusterController(ClusterDomain bl) : BaseController<ClusterDomain>(bl)
{
    [HttpGet("nodes")]
    [AdministratorAuthorize]
    [EndpointName("get_cluster_nodes")]
    [EndpointDescription("Get list of all nodes in cluster")]
    [EndpointSummary("Get Cluster Nodes")]
    [OkJsonResponse(typeof(List<ClusterNode>))]
    public async Task<ActionResult<List<ClusterNode>>> GetNodes()
    {
        var response = await BusinesLayer.GetNodes();
        return Ok(response);
    }

    [HttpGet("health-check")]
    [AllowAnonymous]
    [EndpointName("get_cluster_health_check")]
    [EndpointDescription("Check the health of all nodes in cluster")]
    [EndpointSummary("Check Cluster Health")]
    [OkTextResponse]
    [ServiceUnavailableResponse]
    public async Task<ActionResult<string>> HealthCheck()
    {
        var response = await BusinesLayer.HealthCheck();
        return Ok(response);
    }

    [HttpGet("max-concurrency")]
    [ViewerAuthorize]
    [EndpointName("get_cluster_max_concurrency")]
    [EndpointDescription("Get the total max concurrency of cluster")]
    [EndpointSummary("Get Cluster Max Concurrency")]
    [OkTextResponse]
    public async Task<ActionResult<int>> MaxConcurrency()
    {
        var response = await BusinesLayer.MaxConcurrency();
        return Ok(response);
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Planar.Controllers;

[ApiController]
[EnableRateLimiting("concurrency")]
public abstract class BaseController<TBusinesLayer>(TBusinesLayer businesLayer) : ControllerBase
{
    protected TBusinesLayer BusinesLayer => businesLayer;
}
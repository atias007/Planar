using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Planar.Controllers
{
    [ApiController]
    [EnableRateLimiting("concurrency")]
    public abstract class BaseController<TBusinesLayer> : ControllerBase
    {
        private readonly TBusinesLayer _businesLayer;

        protected BaseController(TBusinesLayer businesLayer)
        {
            _businesLayer = businesLayer;
        }

        protected TBusinesLayer BusinesLayer => _businesLayer;
    }
}
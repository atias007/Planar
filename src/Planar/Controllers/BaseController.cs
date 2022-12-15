using Microsoft.AspNetCore.Mvc;

namespace Planar.Controllers
{
    [ApiController]
    [Produces("application/json")]
    public abstract class BaseController<TBusinesLayer> : ControllerBase
    {
        private readonly TBusinesLayer _businesLayer;

        public BaseController(TBusinesLayer businesLayer)
        {
            _businesLayer = businesLayer;
        }

        protected TBusinesLayer BusinesLayer => _businesLayer;
    }
}
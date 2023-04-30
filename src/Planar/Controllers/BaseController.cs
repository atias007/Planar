using Microsoft.AspNetCore.Mvc;
using Planar.Service.Model.DataObjects;
using System;
using System.Linq;
using System.Security.Claims;

namespace Planar.Controllers
{
    [ApiController]
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
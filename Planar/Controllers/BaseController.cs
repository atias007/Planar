using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Service;
using Planar.Service.Exceptions;
using Quartz;
using System;
using System.Linq;

namespace Planar.Controllers
{
    public class BaseController<TController, TBusinesLayer> : ControllerBase
    {
        protected readonly ILogger<TController> _loggger;
        protected readonly TBusinesLayer _businesLayer;
        protected readonly IServiceProvider _serviceProvider;

        public BaseController(ILogger<TController> logger, IServiceProvider serviceProvider)
        {
            _loggger = logger ?? throw new NullReferenceException(nameof(logger)); ;
            _serviceProvider = serviceProvider ?? throw new NullReferenceException(nameof(serviceProvider)); ;
            _businesLayer = serviceProvider.GetRequiredService<TBusinesLayer>();
        }

        protected static IScheduler Scheduler
        {
            get
            {
                return MainService.Scheduler;
            }
        }

        protected T Resolve<T>()
        {
            return _serviceProvider.GetRequiredService<T>();
        }

        protected ILogger<TController> Logger => _loggger;

        protected TBusinesLayer BusinesLayer => _businesLayer;
    }
}
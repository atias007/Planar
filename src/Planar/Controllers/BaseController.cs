using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Service;
using Quartz;
using System;

namespace Planar.Controllers
{
    public class BaseController<TController, TBusinesLayer> : ControllerBase
    {
        private readonly ILogger<TController> _loggger;
        private readonly TBusinesLayer _businesLayer;
        private readonly IServiceProvider _serviceProvider;

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
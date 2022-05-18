using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using Quartz;
using System;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public abstract class BaseBL<TBusinesLayer>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TBusinesLayer> _logger;
        private readonly DataLayer _dataLayer;

        public BaseBL(ILogger<TBusinesLayer> logger, IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new NullReferenceException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new NullReferenceException(nameof(serviceProvider));
            _dataLayer = serviceProvider.GetRequiredService<DataLayer>();
        }

        protected static IScheduler Scheduler
        {
            get
            {
                return MainService.Scheduler;
            }
        }

        protected DataLayer DataLayer => _dataLayer;

        protected ILogger<TBusinesLayer> Logger => _logger;

        protected T Resolve<T>()
        {
            return _serviceProvider.GetRequiredService<T>();
        }

        protected static T DeserializeObject<T>(string json)
        {
            var entity = JsonConvert.DeserializeObject<T>(json);
            return entity;
        }

        protected static string SerializeObject(object obj)
        {
            var entity = JsonConvert.SerializeObject(obj);
            return entity;
        }
    }
}
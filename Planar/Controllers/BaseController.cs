using Microsoft.AspNetCore.Mvc;
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

        public BaseController(ILogger<TController> logger, TBusinesLayer businesLayer)
        {
            _loggger = logger;
            _businesLayer = businesLayer;
        }

        protected static IScheduler Scheduler
        {
            get
            {
                return MainService.Scheduler;
            }
        }

        protected ILogger<TController> Logger => _loggger;

        protected TBusinesLayer BusinesLayer => _businesLayer;

        protected void ValidateEntity<T>(T entity)
            where T : class
        {
            try
            {
                var properties = typeof(T).GetProperties();
                foreach (var prop in properties)
                {
                    var attributes = prop.GetCustomAttributes(true);
                    foreach (var att in attributes)
                    {
                        if (att is API.Common.Validation.ValidationBaseAttribute validationAtt)
                        {
                            var sourceTypeName = att.GetType().Name.Split('.').Last();
                            var targetTypeName = $"{"Planar.Service.Api.Validation"}.{sourceTypeName}";
                            var validationType = GetType().Assembly.GetType(targetTypeName);
                            if (validationType != null)
                            {
                                if (Activator.CreateInstance(validationType) is Service.Api.Validation.ValidationBaseAttribute instance)
                                {
                                    instance = JsonMapper.Map(att, validationType) as Service.Api.Validation.ValidationBaseAttribute;
                                    var value = prop.GetValue(entity);
                                    instance.Validate(value, prop);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is PlanarValidationException) { throw; }
            }
        }
    }
}
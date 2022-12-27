using AutoMapper;
using CommonJob;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Quartz;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public abstract class BaseBL<TBusinesLayer, TDataLayer> : BaseBL<TBusinesLayer>
        where TDataLayer : BaseDataLayer
    {
        private readonly TDataLayer _dataLayer;

        protected BaseBL(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _dataLayer = serviceProvider.GetRequiredService<TDataLayer>();
        }

        protected TDataLayer DataLayer => _dataLayer;
    }

    public abstract class BaseBL<TBusinesLayer>
    {
        protected readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TBusinesLayer> _logger;
        private readonly SchedulerUtil _schedulerUtil;

        protected BaseBL(IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<TBusinesLayer>>();
            _serviceProvider = serviceProvider ?? throw new PlanarJobException(nameof(serviceProvider));
            _schedulerUtil = serviceProvider.GetRequiredService<SchedulerUtil>();
        }

        protected ClusterUtil ClusterUtil
        {
            get
            {
                var util = _serviceProvider.GetRequiredService<ClusterUtil>();
                return util;
            }
        }

        protected IScheduler Scheduler => _schedulerUtil.Scheduler;

        protected SchedulerUtil SchedulerUtil => _schedulerUtil;

        protected JobKeyHelper JobKeyHelper => _serviceProvider.GetRequiredService<JobKeyHelper>();

        protected TriggerKeyHelper TriggerKeyHelper => _serviceProvider.GetRequiredService<TriggerKeyHelper>();

        protected string ServiceVersion
        {
            get
            {
                var versionString = Assembly.GetEntryAssembly()
                                       ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                       ?.InformationalVersion
                                       .ToString();
                return versionString;
            }
        }

        protected ILogger<TBusinesLayer> Logger => _logger;

        protected IMapper Mapper => _serviceProvider.GetRequiredService<IMapper>();

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

        protected static void ValidateExistingEntity<T>(T entity, string entityName)
            where T : class
        {
            if (entity == null)
            {
                if (string.IsNullOrEmpty(entityName))
                {
                    entityName = "entity";
                }

                throw new RestNotFoundException($"{entityName} could not be found");
            }
        }

        protected async Task ValidateExistingTrigger(TriggerKey entity, string triggerId)
        {
            var details = await Scheduler.GetTrigger(entity);
            if (details == null)
            {
                throw new RestNotFoundException($"trigger with id/key {triggerId} could not be found");
            }
        }

        protected static async Task SetEntityProperties<T>(T entity, UpdateEntityRecord request, IValidator<T> validator = null)
        {
            var type = typeof(T);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var prop = properties.FirstOrDefault(p => string.Compare(p.Name, request.PropertyName, true) == 0);
            if (prop == null)
            {
                throw new RestValidationException("propertyName", $"property name '{request.PropertyName}' could not be found");
            }

            try
            {
                var stringValue = request.PropertyValue;
                if (stringValue.ToLower() == "[null]") { stringValue = null; }
                var propertyType = prop.PropertyType;

                if (Nullable.GetUnderlyingType(propertyType) != null && stringValue != null)
                {
                    var value1 = Convert.ChangeType(stringValue, prop.PropertyType.GetGenericArguments()[0]);
                    prop.SetValue(entity, value1);
                }
                else
                {
                    var value2 = Convert.ChangeType(stringValue, prop.PropertyType);
                    prop.SetValue(entity, value2);
                }
            }
            catch (Exception ex)
            {
                throw new RestValidationException($"property value", $"property value '{request.PropertyValue}' could not be set. ({ex.Message})");
            }

            if (validator != null)
            {
                await validator.ValidateAndThrowAsync(entity);
            }
        }
    }
}
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Quartz;
using System;
using System.Linq;
using System.Reflection;
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

        protected static void ValidateExistingEntity<T>(T entity)
            where T : class
        {
            if (entity == null)
            {
                throw new RestNotFoundException($"{typeof(T).Name} entity could not be found");
            }
        }

        protected void ValidateIdConflict(int routeId, int bodyId)
        {
            if (routeId != bodyId)
            {
                throw new RestValidationException("id", $"conflict id value. (route id: {routeId}, body id: {bodyId}");
            }
        }

        protected void ValidateForbiddenUpdateProperties(UpdateEntityRecord entity, params string[] properties)
        {
            if (properties != null && entity != null)
            {
                if (properties.Any(p => string.Compare(p, entity.PropertyName, true) == 0))
                {
                    throw new RestValidationException(entity.PropertyName, $"property {entity.PropertyName} not allowed to be updated");
                }
            }
        }

        protected static async Task UpdateEntity<T>(T entity, UpdateEntityRecord request, AbstractValidator<T> validator = null)
        {
            var type = typeof(T);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var prop = properties.FirstOrDefault(p => string.Compare(p.Name, request.PropertyName, true) == 0);
            if (prop == null)
            {
                throw new RestValidationException("propertyName", $"property name '{request.PropertyName}' could not be found in {type.Name} entity");
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
                throw new RestValidationException($"property value", $"property value '{request.PropertyValue}' could not be set to property name '{request.PropertyName}' ({ex.Message})");
            }

            if (validator != null)
            {
                await validator.ValidateAndThrowAsync(entity);
            }
        }
    }
}
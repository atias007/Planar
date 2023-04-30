using AutoMapper;
using CommonJob;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar.API.Common.Entities;
using Planar.Common.Helpers;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Planar.Service.Model.DataObjects;
using Planar.Service.Model;
using Quartz;
using System;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Planar.Service.API
{
    public abstract class BaseBL<TBusinesLayer, TDataLayer> : BaseBL<TBusinesLayer>
        where TDataLayer : BaseDataLayer
    {
        private readonly TDataLayer _dataLayer;
        private readonly IHttpContextAccessor _contextAccessor;

        protected BaseBL(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _dataLayer = serviceProvider.GetRequiredService<TDataLayer>();
            _contextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        }

        protected int? UserId
        {
            get
            {
                return GetClaimIntValue(ClaimTypes.NameIdentifier);
            }
        }

        protected Roles RoleId
        {
            get
            {
                var value = GetClaimIntValue(ClaimTypes.Role) ?? 0;
                if (Enum.IsDefined(typeof(Roles), value))
                {
                    return (Roles)value;
                }
                else
                {
                    return Roles.Anonymous;
                }
            }
        }

        private int? GetClaimIntValue(string claimType)
        {
            var context = _contextAccessor.HttpContext;
            if (context?.User?.Claims == null) { return null; }
            var claim = context.User.Claims.FirstOrDefault(c => c.Type == claimType);
            if (claim == null) { return null; }
            var strValue = claim.Value;
            if (string.IsNullOrEmpty(strValue)) { return null; }
            if (!int.TryParse(strValue, out var value)) { return null; }
            return value;
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

        protected string? ServiceVersion
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
            where T : notnull
        {
            return _serviceProvider.GetRequiredService<T>();
        }

        protected static T ValidateExistingEntity<T>(T? entity, string entityName)
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

            return entity;
        }

        protected async Task ValidateExistingTrigger(TriggerKey entity, string triggerId)
        {
            _ = await Scheduler.GetTrigger(entity) ?? throw new RestNotFoundException($"trigger with id/key {triggerId} could not be found");
        }

        protected static async Task SetEntityProperties<T>(T entity, UpdateEntityRequest request, IValidator<T>? validator = null)
        {
            if (request.PropertyValue == null) { return; }

            var type = typeof(T);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var prop =
                properties.FirstOrDefault(p => string.Compare(p.Name, request.PropertyName, true) == 0) ??
                throw new RestValidationException("propertyName", $"property name '{request.PropertyName}' could not be found");

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
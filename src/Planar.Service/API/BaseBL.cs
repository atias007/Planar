﻿using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.API.Helpers;
using Planar.Service.Audit;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Quartz;
using System;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Planar.Service.API;

public abstract class BaseBL<TBusinesLayer, TDataLayer>(IServiceProvider serviceProvider) : BaseBL<TBusinesLayer>(serviceProvider)
    where TDataLayer : IBaseDataLayer
{
    private readonly TDataLayer _dataLayer = serviceProvider.GetRequiredService<TDataLayer>();
    private readonly IHttpContextAccessor _contextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();

    protected int? UserId
    {
        get
        {
            return GetClaimIntValue(ClaimTypes.NameIdentifier);
        }
    }

    protected Roles UserRole
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
        if (!int.TryParse(strValue, out int value)) { return null; }
        return value;
    }

    protected TDataLayer DataLayer => _dataLayer;
}

public abstract class BaseBL<TBusinesLayer>(IServiceProvider serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new PlanarJobException(nameof(serviceProvider));
    private readonly ILogger<TBusinesLayer> _logger = serviceProvider.GetRequiredService<ILogger<TBusinesLayer>>();
    private readonly SchedulerUtil _schedulerUtil = serviceProvider.GetRequiredService<SchedulerUtil>();

    protected IServiceProvider ServiceProvider => _serviceProvider;

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

#pragma warning disable S2325 // Methods and properties that don't access instance data should be static

    protected string? ServiceVersion
#pragma warning restore S2325 // Methods and properties that don't access instance data should be static
    {
        get
        {
            var versionString = Assembly.GetEntryAssembly()
                                   ?.GetCustomAttribute<AssemblyFileVersionAttribute>()
                                   ?.Version;
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

    protected async Task<ITrigger> ValidateExistingTrigger(TriggerKey entity, string triggerId)
    {
        return await Scheduler.GetTrigger(entity) ?? throw new RestNotFoundException($"trigger with id '{triggerId}' could not be found");
    }

    protected void AuditSecuritySafe(string title, bool isWarning = false)
    {
        try
        {
            var audit = new SecurityMessage
            {
                Title = title,
                IsWarning = isWarning
            };

            AuditSecurityInner(audit);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "fail to publish security audit with title {Title}. is warning audit: {IsWarning}", title, isWarning);
        }
    }

    private void AuditSecurityInner(SecurityMessage audit)
    {
        var context = Resolve<IHttpContextAccessor>();
        var claims = context?.HttpContext?.User?.Claims;
        audit.Claims = claims;
        var producer = Resolve<SecurityProducer>();
        producer.Publish(audit);
    }

    protected static void TrimPropertyName(UpdateEntityRequestByName requestByName)
    {
        const char space = ' ';
        while (requestByName.PropertyName.Contains(space))
        {
            requestByName.PropertyName = requestByName.PropertyName.Replace(space.ToString(), string.Empty);
        }
    }

    protected static void TrimPropertyName(UpdateEntityRequestById requestByName)
    {
        const char space = ' ';
        while (requestByName.PropertyName.Contains(space))
        {
            requestByName.PropertyName = requestByName.PropertyName.Replace(space.ToString(), string.Empty);
        }
    }

    protected static void ForbbidenPartialUpdateProperties(UpdateEntityRequest request, string? message, params string[] properties)
    {
        var any = Array.Exists(properties, p => string.Equals(request.PropertyName, p, StringComparison.OrdinalIgnoreCase));
        if (any)
        {
            var errorMessage = $"property '{request.PropertyName}' can not be updated";
            if (string.IsNullOrEmpty(message))
            {
                throw new RestValidationException("property name", errorMessage);
            }

            throw new RestValidationException("property name", errorMessage, errorMessage, suggestion: message);
        }
    }

    protected static async Task SetEntityProperties<T>(T entity, UpdateEntityRequest request, IValidator<T>? validator = null)
    {
        ForbbidenPartialUpdateProperties(request, null, "id");
        if (request.PropertyValue == null) { return; }

        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
        var prop =
            properties.Find(p => string.Compare(p.Name, request.PropertyName, true) == 0) ??
            throw new RestValidationException("propertyName", $"property name '{request.PropertyName}' could not be found");

        try
        {
            var stringValue = request.PropertyValue;
            if (stringValue.Equals("[null]", StringComparison.CurrentCultureIgnoreCase)) { stringValue = null; }
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
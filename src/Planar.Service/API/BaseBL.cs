using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.API.Helpers;
using Planar.Service.Audit;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Planar.Service.API;

public abstract class BaseBL<TBusinesLayer, TDataLayer>(IServiceProvider serviceProvider) : BaseBL<TBusinesLayer>(serviceProvider)
    where TDataLayer : IBaseDataLayer
{
    private readonly TDataLayer _dataLayer = serviceProvider.GetRequiredService<TDataLayer>();
    protected TDataLayer DataLayer => _dataLayer;
}

public abstract class BaseBL<TBusinesLayer>(IServiceProvider serviceProvider)
{
    private readonly IHttpContextAccessor _contextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
    private readonly ILogger<TBusinesLayer> _logger = serviceProvider.GetRequiredService<ILogger<TBusinesLayer>>();
    private readonly SchedulerUtil _schedulerUtil = serviceProvider.GetRequiredService<SchedulerUtil>();
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new PlanarJobException(nameof(serviceProvider));

    protected ClusterUtil ClusterUtil
    {
        get
        {
            var util = _serviceProvider.GetRequiredService<ClusterUtil>();
            return util;
        }
    }

    protected JobKeyHelper JobKeyHelper => _serviceProvider.GetRequiredService<JobKeyHelper>();

    protected ILogger<TBusinesLayer> Logger => _logger;

    protected IMapper Mapper => _serviceProvider.GetRequiredService<IMapper>();

    protected SchedulerUtil SchedulerUtil => _schedulerUtil;

    protected IServiceProvider ServiceProvider => _serviceProvider;

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

    protected static long ValidateExistingLong(long value, string entityName)
    {
        if (value == 0)
        {
            if (string.IsNullOrEmpty(entityName))
            {
                entityName = "entity";
            }

            throw new RestNotFoundException($"{entityName} could not be found");
        }

        return value;
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

    protected void AuditSecuritySafe(string title, bool isWarning = false)
    {
        try
        {
            var audit = GetAuditSecurityMessage(title, isWarning);
            if (audit == null) { return; }
            var producer = Resolve<SecurityProducer>();
            producer.Publish(audit);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "fail to publish security audit with title {Title}. is warning audit: {IsWarning}", title, isWarning);
        }
    }

    protected void AuditSecuritySafe(SecurityMessage message)
    {
        try
        {
            if (message == null) { return; }
            var producer = Resolve<SecurityProducer>();
            producer.Publish(message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "fail to publish security audit with title {Title}. is warning audit: {IsWarning}", message.Title, message.IsWarning);
        }
    }

    protected SecurityMessage? GetAuditSecurityMessage(string title, bool isWarning = false)
    {
        try
        {
            var context = Resolve<IHttpContextAccessor>();

            var audit = new SecurityMessage(context)
            {
                Title = title,
                IsWarning = isWarning
            };

            return audit;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "fail to get security audit message with title {Title}. is warning audit: {IsWarning}", title, isWarning);
            return null;
        }
    }

    protected int? GetClaimIntValue(string claimType)
    {
        var context = _contextAccessor.HttpContext;
        if (context?.User?.Claims == null) { return null; }
        var claim = context.User.Claims.FirstOrDefault(c => c.Type == claimType);
        if (claim == null) { return null; }
        var strValue = claim.Value;
        if (string.IsNullOrWhiteSpace(strValue)) { return null; }
        if (int.TryParse(strValue, out int value)) { return value; }
        return RoleHelper.GetRoleValue(strValue);
    }

    protected async Task<IScheduler> GetScheduler() => await _schedulerUtil.SchedulerFactory.GetScheduler();

    protected T Resolve<T>()
        where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    protected T? ResolveOptionally<T>()
        where T : notnull
    {
        return _serviceProvider.GetService<T>();
    }

    protected async Task<ITrigger> ValidateExistingTrigger(TriggerKey entity, string triggerId)
    {
        var scheduler = await GetScheduler();
        return await scheduler.GetTrigger(entity) ?? throw new RestNotFoundException($"trigger with id '{triggerId}' could not be found");
    }

    protected static async Task<IEnumerable<KeyValuePair<string, string>>> GetApplyYamls(HttpContext httpContext, string kind)
    {
        // Validate YAML content type
        var contentType = httpContext.Request.ContentType ?? string.Empty;
        if (!contentType.Contains("yaml", StringComparison.OrdinalIgnoreCase))
        {
            throw new RestValidationException("contentType", $"Unsupported content type: {contentType}");
        }

        // Read body content
        using var reader = new StreamReader(httpContext.Request.Body);
        var content = await reader.ReadToEndAsync(httpContext.RequestAborted);
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new RestValidationException("request body", "request body is empty");
        }

        var result = new List<KeyValuePair<string, string>>();
        try
        {
            var yamls = YmlUtil.SplitByKind(content);
            for (var i = 0; i < yamls.Count; i++)
            {
                var yml = yamls[i];
                ValidateKind(kind, yml.Key);
                if (string.IsNullOrWhiteSpace(yml.Value)) { continue; }
                result.Add(new KeyValuePair<string, string>(yml.Key, yml.Value));
            }
        }
        catch (Exception ex)
        {
            var source = YmlUtil.GetApplySource(content);
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new RestValidationException("yaml", $"fail to map body to {kind} request\r\n{ex.Message}");
            }
            else
            {
                throw new RestValidationException(source, $"fail to map content of file: {source} to {kind} request\r\n{ex.Message}");
            }
        }

        return result;
    }

    protected async Task<IEnumerable<T>> GetApplyEntities<T>(HttpContext httpContext, string kind, bool withValidation = true)
        where T : class, IApplyRequest, new()
    {
        var yamls = await GetApplyYamls(httpContext, kind);
        var validator = withValidation ? ResolveOptionally<IValidator<T>>() : null;
        var entities = new List<T>();

        try
        {
            foreach (var y in yamls)
            {
                var entity = YmlUtil.Deserialize<T>(y.Value);
                if (entity == null) { continue; }
                if (validator != null)
                {
                    await validator.ValidateAndThrowAsync(entity, httpContext.RequestAborted);
                }

                entities.Add(entity);
            }
        }
        catch (RestValidationException)
        {
            throw;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RestValidationException("yaml", $"Fail to map yaml body to {kind} request\r\n{ex.Message}");
        }

        return entities;
    }

    private static void ValidateKind(string kind, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new RestValidationException("kind", "kind property is missing of empty");
        }

        if (key != kind)
        {
            throw new RestValidationException("kind", $"Unexpected kind: {key}. Expected kind: {kind}");
        }
    }
}
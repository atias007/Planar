using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Planner.API.Common.Entities;
using Planner.Service;
using Planner.Service.Exceptions;
using Quartz;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Planner.Controllers
{
    public class BaseController : ControllerBase
    {
        protected ILogger _loggger;

        public BaseController(ILogger logger)
        {
            _loggger = logger;
        }

        protected void HandleException(Exception ex)
        {
            HandleException<BaseResponse>(ex);
        }

        protected void HandleException<T>(Exception ex)
        {
            if (ex is AggregateException aggex)
            {
                foreach (var item in aggex.InnerExceptions)
                {
                    LogException(item);
                    //result.ErrorDescription += $"{item.Message}\r\n";
                }

                //result.ErrorDescription = result.ErrorDescription.Trim();
            }
            else
            {
                LogException(ex);
                //result.ErrorDescription = ex.Message;
            }
        }

        protected static IScheduler Scheduler
        {
            get
            {
                return MainService.Scheduler;
            }
        }

        private void LogException(Exception ex)
        {
            var message = "API path {@path} fail: {@message}";
            var path = HttpContext.Request.Path.Value;

            if (ex is PlannerValidationException || ex is ValidationException)
            {
                _loggger.LogWarning(message, path, ex.Message);
            }
            else
            {
                _loggger.LogError(ex, message, path, ex.Message);
            }
        }

        protected void InitializeService()
        {
            _loggger.LogInformation("API path {@method} invoked", HttpContext.Request.Path.Value);
        }

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
                            var targetTypeName = $"{"Planner.Service.Api.Validation"}.{sourceTypeName}";
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
                if (ex is PlannerValidationException) { throw; }
            }
        }
    }
}
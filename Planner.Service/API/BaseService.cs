using FluentValidation;
using Planner.API.Common.Entities;
using Planner.Service.Api.Validation;
using Planner.Service.Exceptions;
using Serilog;
using System;
using System.Linq;

namespace Planner.Service.API
{
    public class BaseService
    {
        protected ILogger _loggger;

        public BaseService(ILogger logger)
        {
            _loggger = logger;
        }

        protected BaseResponse HandleException(Exception ex, string method)
        {
            return HandleException<BaseResponse>(ex, method);
        }

        protected T HandleException<T>(Exception ex, string method)
            where T : BaseResponse
        {
            var result = Activator.CreateInstance<T>();
            result.Success = false;
            result.ErrorCode = -1;

            if (ex is AggregateException aggex)
            {
                foreach (var item in aggex.InnerExceptions)
                {
                    LogException(item, method);
                    result.ErrorDescription += $"{item.Message}\r\n";
                }

                result.ErrorDescription = result.ErrorDescription.Trim();
            }
            else
            {
                LogException(ex, method);
                result.ErrorDescription = ex.Message;
            }

            return result;
        }

        private void LogException(Exception ex, string method)
        {
            var message = "API method {@method} fail: {@message}";

            if (ex is PlannerValidationException || ex is ValidationException)
            {
                _loggger.Warning(message, method, ex.Message);
            }
            else
            {
                _loggger.Error(ex, message, method, ex.Message);
            }
        }

        protected void InitializeService(string method)
        {
            _loggger.Information("API method {@method} invoked", method);
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
                        if (att is Planner.API.Common.Validation.ValidationBaseAttribute validationAtt)
                        {
                            var sourceTypeName = att.GetType().Name.Split('.').Last();
                            var targetTypeName = $"{"Planner.Service.Api.Validation"}.{sourceTypeName}";
                            var validationType = GetType().Assembly.GetType(targetTypeName);
                            if (validationType != null)
                            {
                                if (Activator.CreateInstance(validationType) is ValidationBaseAttribute instance)
                                {
                                    instance = JsonMapper.Map(att, validationType) as ValidationBaseAttribute;
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
﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Planar.Service.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Planar.Filters
{
    public class HttpResponseExceptionFilter : IActionFilter, IOrderedFilter
    {
        public int Order => int.MaxValue - 10;

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Do nothing //
        }

        private static RestValidationException AggregateRestValidationException(IEnumerable<RestValidationException> exceptions)
        {
            var result = new RestValidationException();
            var problems = exceptions.SelectMany(e => e.Errors).ToList();

            problems.ForEach(i =>
            {
                result.Errors.Add(i);
            });

            return result;
        }

        private static void HandleValidationException(ActionExecutedContext context, RestValidationException exception)
        {
            const string ProblemType = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
            const string MultipleErrors = "One or more validation errors occurred.";

            RestBadRequestResult problem;
            if (exception.TotalErrors == 1)
            {
                var error = exception.Errors.First();
                problem = new RestBadRequestResult
                {
                    Detail = error.Detail.FirstOrDefault(),
                    Instance = context.HttpContext.Request.Path,
                    Status = StatusCodes.Status400BadRequest,
                    Title = error.Field,
                    Type = ProblemType,
                    ErrorCode = error.ErrorCode,
                };
            }
            else
            {
                problem = new RestBadRequestResult
                {
                    Instance = context.HttpContext.Request.Path,
                    Status = StatusCodes.Status400BadRequest,
                    Title = MultipleErrors,
                    Type = ProblemType,
                    Errors = exception.Errors
                        .Select(e => new RestBadRequestError { Field = e.Field, Detail = e.Detail })
                        .ToList(),
                };
            }

            context.Result = new BadRequestObjectResult(problem);

            if (!string.IsNullOrWhiteSpace(exception.ClientMessage))
            {
                var base64ClientMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(exception.ClientMessage));
                context.HttpContext.Response.Headers.Append(Consts.CliMessageHeaderName, base64ClientMessage);
            }

            if (!string.IsNullOrWhiteSpace(exception.Suggestion))
            {
                var base64Suggestion = Convert.ToBase64String(Encoding.UTF8.GetBytes(exception.Suggestion));
                context.HttpContext.Response.Headers.Append(Consts.CliSuggestionHeaderName, base64Suggestion);
            }

            context.ExceptionHandled = true;
        }

        private static void HandleValidationException(ActionExecutedContext context, FluentValidation.ValidationException exception)
        {
            const string ProblemType = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
            const string MultipleErrors = "One or more validation errors occurred.";

            RestBadRequestResult problem;
            if (exception.Errors.Count() == 1)
            {
                var error = exception.Errors.First();
                problem = new RestBadRequestResult
                {
                    Detail = error.ErrorMessage,
                    Instance = context.HttpContext.Request.Path,
                    Status = StatusCodes.Status400BadRequest,
                    Title = error.PropertyName,
                    Type = ProblemType,
                };
            }
            else
            {
                problem = new RestBadRequestResult
                {
                    Instance = context.HttpContext.Request.Path,
                    Status = StatusCodes.Status400BadRequest,
                    Title = MultipleErrors,
                    Type = ProblemType,
                    Errors = exception.Errors
                        .Select(e => new RestBadRequestError { Field = e.PropertyName, Detail = [e.ErrorMessage] })
                        .ToList(),
                };
            }

            context.Result = new BadRequestObjectResult(problem);
            context.ExceptionHandled = true;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception is RestServiceUnavailableException unavailableException)
            {
                HandleRestUnavailableException(context, unavailableException);
                return;
            }

            if (context.Exception is RestNotFoundException notFoundException)
            {
                context.Result = new NotFoundObjectResult(notFoundException.Value);
                context.ExceptionHandled = true;
                return;
            }

            if (context.Exception is RestRequestTimeoutException timeoutException)
            {
                context.Result = new RequestTimeoutObjectResult(timeoutException.Value);
                context.ExceptionHandled = true;
                return;
            }

            if (context.Exception is RestForbiddenException)
            {
                context.Result = new ForbidResult();
                context.ExceptionHandled = true;
                return;
            }

            if (context.Exception is RestConflictException conflictException)
            {
                context.Result = new ConflictObjectResult(conflictException.Value);
                context.ExceptionHandled = true;
                return;
            }

            if (context.Exception is RestValidationException validationException)
            {
                HandleValidationException(context, validationException);
                return;
            }

            if (context.Exception is FluentValidation.ValidationException validationException2)
            {
                HandleValidationException(context, validationException2);
                return;
            }

            if (context.Exception is RestGeneralException generalException)
            {
                HandleRestGeneralException(context, generalException);
                return;
            }

            if (context.Exception is AggregateException aggregateException)
            {
                HandleAggregateException(context, aggregateException);
            }
        }

        private static void HandleRestUnavailableException(ActionExecutedContext context, RestServiceUnavailableException unavailableException)
        {
            context.Result = new ObjectResult(unavailableException.Message)
            {
                StatusCode = 503
            };

            context.ExceptionHandled = true;
        }

        private static void HandleRestGeneralException(ActionExecutedContext context, RestGeneralException generalException)
        {
            context.Result = new ObjectResult(generalException.ToString())
            {
                StatusCode = generalException.StatusCode ?? StatusCodes.Status500InternalServerError,
            };

            context.ExceptionHandled = generalException.StatusCode < 500 || generalException.StatusCode >= 600;
        }

        private static void HandleAggregateException(ActionExecutedContext context, AggregateException aggregateException)
        {
            var all = aggregateException.Flatten();
            if (all.InnerExceptions.All(e => e is RestValidationException))
            {
                var allRestEx = all.InnerExceptions.Cast<RestValidationException>().ToList();

                if (allRestEx.Count == 1)
                {
                    HandleValidationException(context, allRestEx[0]);
                }
                else
                {
                    var aggregate = AggregateRestValidationException(allRestEx);
                    HandleValidationException(context, aggregate);
                }
            }
            else
            {
                var filterException = all.InnerExceptions.Where(e => e is not RestValidationException).ToList();
                throw new AggregateException(filterException);
            }
        }
    }
}
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Planar.Filters;

public sealed partial class FluentValidationActionFilter(ProblemDetailsFactory problemDetailsFactory)
    : IAsyncActionFilter
{
    // Argument type -> IValidator<ArgumentType>. Built once per type for the process lifetime.
    private static readonly ConcurrentDictionary<Type, Type> ValidatorTypeCache = new();

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        var ct = context.HttpContext.RequestAborted;
        var failures = new List<ValidationFailure>();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) { continue; }

            var validatorType = ValidatorTypeCache.GetOrAdd(
                argument.GetType(),
                static t => typeof(IValidator<>).MakeGenericType(t));

            if (services.GetService(validatorType) is not IValidator validator) { continue; }

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument), ct);
            if (!result.IsValid) { failures.AddRange(result.Errors); }
        }

        if (failures.Count == 0)
        {
            await next();
            return;
        }

        var problem = problemDetailsFactory.CreateProblemDetails(
            context.HttpContext,
            statusCode: StatusCodes.Status400BadRequest,
            title: BadRequestUtil.GetTitle(failures),
            type: BadRequestUtil.ProblemType,
            detail: BadRequestUtil.GetDetail(failures),
            instance: context.HttpContext.Request.Path);

        if (failures.Count > 1)
        {
            problem.Extensions["errors"] = failures.Select(f => new
            {
                detail = new string[] { f.ErrorMessage },
                field = f.PropertyName,
                code = f.ErrorCode
            });
        }

        context.Result = new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { MediaTypeNames.Application.Json }
        };
    }
}
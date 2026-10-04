using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Planar.Filters;

internal static class BadRequestUtil
{
    public const string ProblemType = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
    private const string MultipleErrors = "One or more validation errors occurred.";
    private const string DetailTemplate = "The request contains {0} invalid field(s).";

    public static string GetTitle(List<ValidationFailure> failures)
    {
        if (failures.Count == 1)
        {
            return $"{failures[0].PropertyName} is invalid";
        }

        return MultipleErrors;
    }

    public static string GetDetail(List<ValidationFailure> failures)
    {
        if (failures.Count == 1)
        {
            return failures[0].ErrorMessage;
        }

        return string.Format(CultureInfo.CurrentCulture, DetailTemplate, failures.Count);
    }

    public static BadRequestObjectResult CreateCustomErrorResponse(ActionContext context)
    {
        // BadRequestObjectResult is class found Microsoft.AspNetCore.Mvc and is inherited from ObjectResult.
        var single = context.ModelState.Count == 1 && context.ModelState.First().Value.Errors.Count == 1;

        RestBadRequestResult result;

        if (single)
        {
            var error = context.ModelState.First();
            const string titleTemplate = "{0} is invalid";

            var first = error.Value.Errors[0];
            result = new RestBadRequestResult
            {
                Instance = context.HttpContext.Request.Path,
                Status = StatusCodes.Status400BadRequest,
                Title = string.Format(CultureInfo.CurrentCulture, titleTemplate, error.Key),
                Type = ProblemType,
                Detail = first.ErrorMessage,
                Errors = [],
            };
        }
        else
        {
            result = new RestBadRequestResult
            {
                Instance = context.HttpContext.Request.Path,
                Status = StatusCodes.Status400BadRequest,
                Title = MultipleErrors,
                Type = ProblemType,
                Detail = string.Format(CultureInfo.CurrentCulture, DetailTemplate, context.ModelState.Count),
                Errors = [.. context.ModelState
                            .Where(v => v.Value.Errors.Any())
                            .Select(v => new RestBadRequestError
                            {
                                Field = v.Key,
                                Detail = [.. v.Value.Errors.Select(e => e.ErrorMessage)],
                            })]
            };
        }

        return new BadRequestObjectResult(result);
    }
}
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Linq;

namespace Planar.Filters
{
    internal static class BadRequestUtil
    {
        public static BadRequestObjectResult CreateCustomErrorResponse(ActionContext context)
        {
            const string ProblemType = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
            const string MultipleErrors = "One or more validation errors occurred.";

            // BadRequestObjectResult is class found Microsoft.AspNetCore.Mvc and is inherited from ObjectResult.
            var single = context.ModelState.Count == 1 && context.ModelState.First().Value.Errors.Count == 1;

            RestBadRequestResult result;

            if (single)
            {
                var error = context.ModelState.First();
                const string titleTemplate = "{0} is invalid";

                var first = error.Value.Errors.First();
                result = new RestBadRequestResult
                {
                    Instance = context.HttpContext.Request.Path,
                    Status = StatusCodes.Status400BadRequest,
                    Title = string.Format(CultureInfo.CurrentCulture, titleTemplate, error.Key),
                    Type = ProblemType,
                    Detail = first.ErrorMessage,
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
                    Errors = context.ModelState
                                .Where(v => v.Value.Errors.Any())
                                .Select(v => new RestBadRequestError
                                {
                                    Field = v.Key,
                                    Detail = v.Value.Errors.Select(e => e.ErrorMessage).ToList()
                                }).ToList()
                };
            }

            return new BadRequestObjectResult(result);
        }
    }
}
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace Planar.Filters
{
    [AttributeUsage(AttributeTargets.All)]
    public class ValidateModelStateAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                context.Result = BadRequestUtil.CreateCustomErrorResponse(context);
            }
        }
    }
}
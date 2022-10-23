using Microsoft.AspNetCore.Mvc.Filters;

namespace Planar.Filters
{
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
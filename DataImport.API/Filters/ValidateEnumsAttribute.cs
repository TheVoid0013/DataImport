using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DataImport.API.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ValidateEnumsAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(
                new ValidationProblemDetails(context.ModelState));
            return;
        }
        
        foreach (var (name, value) in context.ActionArguments)
        {
            if (value is null) continue;

            var type = value.GetType();
            if (type.IsEnum && !Enum.IsDefined(type, value))
            {
                context.ModelState.AddModelError(name, $"'{value}' is not a valid {type.Name}.");
                context.Result = new BadRequestObjectResult(
                    new ValidationProblemDetails(context.ModelState));
                return;
            }
        }
    }
}
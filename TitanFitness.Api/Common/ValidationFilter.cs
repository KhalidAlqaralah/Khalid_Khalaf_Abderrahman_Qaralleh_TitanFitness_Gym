using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TitanFitness.Api.Common;

/// <summary>
/// Runs the FluentValidation validator of every request argument (body or [FromQuery] object) before the action.
/// A failed request contract returns 400 with field errors keyed by camelCase property name.
/// </summary>
public sealed class ValidationFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var errors = new Dictionary<string, List<string>>();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (services.GetService(validatorType) is not IValidator validator)
                continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);

            foreach (var failure in result.Errors)
            {
                var key = string.IsNullOrEmpty(failure.PropertyName)
                    ? string.Empty
                    : char.ToLowerInvariant(failure.PropertyName[0]) + failure.PropertyName[1..];

                if (!errors.TryGetValue(key, out var list))
                    errors[key] = list = [];

                list.Add(failure.ErrorMessage);
            }
        }

        if (errors.Count > 0)
        {
            var problem = new ValidationProblemDetails(errors.ToDictionary(e => e.Key, e => e.Value.ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "The request is not valid.",
                Detail = errors.Count == 1 && errors.ContainsKey(string.Empty) ? errors[string.Empty][0] : "One or more fields are not valid.",
                Instance = context.HttpContext.Request.Path
            };

            context.Result = new BadRequestObjectResult(problem);
            return;
        }

        await next();
    }
}

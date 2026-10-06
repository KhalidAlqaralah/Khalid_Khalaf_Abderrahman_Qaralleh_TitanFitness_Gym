using Microsoft.AspNetCore.Mvc;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Api.Common;

/// <summary>
/// Turns a failed <see cref="Result"/> into a ProblemDetails response:
/// Validation → 422, NotFound → 404, Conflict → 409. When the error names a field,
/// it is also listed under "errors" so the Angular form can put it under the right control.
/// </summary>
public static class ResultExtensions
{
    public static IActionResult ToProblem(this ControllerBase controller, Error error)
    {
        var status = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status422UnprocessableEntity
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = error.Type switch
            {
                ErrorType.NotFound => "Not found",
                ErrorType.Conflict => "Conflict",
                _ => "Validation failed"
            },
            Detail = error.Message,
            Instance = controller.HttpContext.Request.Path
        };

        problem.Extensions["code"] = error.Code;

        if (error.Field is not null)
            problem.Extensions["errors"] = new Dictionary<string, string[]> { [error.Field] = [error.Message] };

        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }

    public static IActionResult FromResult(this ControllerBase controller, Result result) =>
        result.IsSuccess ? controller.NoContent() : controller.ToProblem(result.Error);

    public static IActionResult FromResult<T>(this ControllerBase controller, Result<T> result) =>
        result.IsSuccess ? controller.Ok(result.Value) : controller.ToProblem(result.Error);

    public static IActionResult Created<T>(this ControllerBase controller, Result<T> result, string actionName, Func<T, object> routeValues) =>
        result.IsSuccess
            ? controller.CreatedAtAction(actionName, routeValues(result.Value), result.Value)
            : controller.ToProblem(result.Error);
}

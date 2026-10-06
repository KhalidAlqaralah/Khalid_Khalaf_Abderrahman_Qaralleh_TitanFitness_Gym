using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TitanFitness.Api.Common;

/// <summary>
/// Last line of defence. Business failures never get here (they are Results); this only handles
/// concurrency and database conflicts (409) and genuine bugs (500).
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflict",
                "This record was changed by someone else. Reload it and try again."),
            DbUpdateException => (StatusCodes.Status409Conflict, "Conflict",
                "The change conflicts with existing data (for example a duplicate value)."),
            BadHttpRequestException bad => (StatusCodes.Status400BadRequest, "Bad request", bad.Message),
            _ => (StatusCodes.Status500InternalServerError, "Server error", "Something went wrong. Please try again.")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception for {Path}", httpContext.Request.Path);
        else
            logger.LogWarning(exception, "Request failed with {Status} for {Path}", status, httpContext.Request.Path);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        if (environment.IsDevelopment() && status == StatusCodes.Status500InternalServerError)
            problem.Extensions["exception"] = exception.Message;

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}

using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TitanFitness.Application.Common;

/// <summary>Logs every command and query with how long it took.</summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        var watch = Stopwatch.StartNew();

        logger.LogInformation("Handling {Request}", name);
        var response = await next(cancellationToken);
        logger.LogInformation("Handled {Request} in {Elapsed} ms", name, watch.ElapsedMilliseconds);

        return response;
    }
}

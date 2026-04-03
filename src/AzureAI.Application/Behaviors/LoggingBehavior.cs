using MediatR;
using Microsoft.Extensions.Logging;

namespace AzureAI.Application.Behaviors;

/// <summary>MediatR pipeline behavior that logs the start and completion of each request.</summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    /// <summary>Initializes a new <see cref="LoggingBehavior{TRequest,TResponse}"/>.</summary>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling {RequestName}", requestName);
        var response = await next();
        _logger.LogInformation("Handled {RequestName}", requestName);
        return response;
    }
}

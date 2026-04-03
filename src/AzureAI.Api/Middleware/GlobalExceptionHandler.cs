using AzureAI.Api.Models;
using AzureAI.Core.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace AzureAI.Api.Middleware;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env    = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, response) = exception switch
        {
            ValidationException vex => (
                StatusCodes.Status400BadRequest,
                ApiResponse.ValidationFail(vex.Errors.Select(e => e.ErrorMessage))),

            DocumentProcessingException dpex => (
                StatusCodes.Status422UnprocessableEntity,
                ApiResponse.Fail(_env.IsDevelopment() ? dpex.Message : "Document processing failed.")),

            EmbeddingGenerationException or CompletionException => (
                StatusCodes.Status502BadGateway,
                ApiResponse.Fail("An upstream AI service error occurred.")),

            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                ApiResponse.Fail(exception.Message)),

            OperationCanceledException => (
                StatusCodes.Status408RequestTimeout,
                ApiResponse.Fail("The request was cancelled.")),

            _ => (
                StatusCodes.Status500InternalServerError,
                ApiResponse.Fail("An unexpected error occurred."))
        };

        if (statusCode >= 500)
            _logger.LogError(exception, "Unhandled {ExceptionType}", exception.GetType().Name);
        else
            _logger.LogWarning(exception, "Handled {ExceptionType}: {Message}", exception.GetType().Name, exception.Message);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}

using Serilog.Context;

namespace AzureAI.Api.Middleware;

internal sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var correlationId)
            || string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("D");
        }

        var id = correlationId.ToString();
        context.Items[HeaderName] = id;
        context.Response.Headers.Append(HeaderName, id);

        using (LogContext.PushProperty("CorrelationId", id))
        {
            await _next(context);
        }
    }
}

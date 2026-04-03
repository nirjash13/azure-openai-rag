using Serilog.Context;

namespace AzureAI.Api.Middleware;

internal sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        string id;

        if (context.Request.Headers.TryGetValue(HeaderName, out var correlationId)
            && !string.IsNullOrWhiteSpace(correlationId))
        {
            // Sanitise: keep only alphanumeric, hyphens, and underscores; cap at 64 chars.
            var raw = correlationId.ToString();
            id = new string(raw.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').Take(64).ToArray());
            if (string.IsNullOrEmpty(id))
                id = Guid.NewGuid().ToString("D");
        }
        else
        {
            id = Guid.NewGuid().ToString("D");
        }

        context.Items[HeaderName] = id;
        context.Response.Headers.Append(HeaderName, id);

        using (LogContext.PushProperty("CorrelationId", id))
        {
            await _next(context);
        }
    }
}

using Polly;
using Polly.Extensions.Http;

namespace AzureAI.Infrastructure.Resilience;

/// <summary>Polly resilience policies for HTTP calls to Azure services.</summary>
public static class RetryPolicies
{
    /// <summary>
    /// Retry policy for transient HTTP errors and 429 (Too Many Requests) responses.
    /// Uses exponential back-off with jitter; honours the <c>Retry-After</c> header when present.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> AzureServiceRetry() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: (attempt, outcome, _) =>
                {
                    if (outcome.Result?.Headers.RetryAfter?.Delta is TimeSpan retryAfter)
                        return retryAfter;

                    var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    var jitter    = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));
                    return baseDelay + jitter;
                },
                onRetryAsync: (_, timespan, attempt, _) =>
                {
                    // Logging is wired at the call site via Polly context if needed.
                    _ = (timespan, attempt);
                    return Task.CompletedTask;
                });
}

using Polly;
using Polly.Extensions.Http;

namespace AzureAI.Infrastructure.Resilience;

/// <summary>Circuit-breaker policies that prevent cascading load on degraded Azure services.</summary>
public static class CircuitBreakerPolicies
{
    /// <summary>
    /// Opens the circuit after five consecutive failures, preventing further calls for 30 seconds,
    /// then allows a single probe request to test whether the service has recovered.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> AzureServiceCircuitBreaker() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
}

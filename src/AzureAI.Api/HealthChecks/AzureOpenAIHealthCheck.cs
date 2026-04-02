using AzureAI.Core.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace AzureAI.Api.HealthChecks;

/// <summary>Verifies connectivity to the configured Azure OpenAI endpoint.</summary>
internal sealed class AzureOpenAIHealthCheck : IHealthCheck
{
    private readonly AzureOpenAISettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;

    public AzureOpenAIHealthCheck(IOptions<AzureOpenAISettings> settings, IHttpClientFactory httpClientFactory)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Endpoint))
            return HealthCheckResult.Degraded("Azure OpenAI endpoint is not configured.");

        try
        {
            using var client = _httpClientFactory.CreateClient();
            var url = $"{_settings.Endpoint.TrimEnd('/')}/openai/models?api-version=2024-02-01";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("api-key", _settings.ApiKey);

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Degraded($"Azure OpenAI returned HTTP {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Azure OpenAI endpoint is unreachable.", ex);
        }
    }
}

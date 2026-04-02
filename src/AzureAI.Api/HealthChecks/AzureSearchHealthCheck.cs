using AzureAI.Core.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace AzureAI.Api.HealthChecks;

/// <summary>Verifies connectivity to the configured Azure AI Search service.</summary>
internal sealed class AzureSearchHealthCheck : IHealthCheck
{
    private readonly AzureSearchSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;

    public AzureSearchHealthCheck(IOptions<AzureSearchSettings> settings, IHttpClientFactory httpClientFactory)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Endpoint))
            return HealthCheckResult.Degraded("Azure AI Search endpoint is not configured.");

        try
        {
            using var client = _httpClientFactory.CreateClient();
            var url = $"{_settings.Endpoint.TrimEnd('/')}/indexes?api-version=2023-11-01";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("api-key", _settings.ApiKey);

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Degraded($"Azure AI Search returned HTTP {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Azure AI Search endpoint is unreachable.", ex);
        }
    }
}

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace AzureAI.Integration.Tests;

public sealed class SearchEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SearchEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostSearch_WithQuery_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/search", new { Query = "test" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

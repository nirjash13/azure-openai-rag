using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace AzureAI.Integration.Tests;

public sealed class ChatEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ChatEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostConversations_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/chat/conversations", new { Title = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostChat_WithQuestion_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/chat", new { Question = "test" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetConversationHistory_WhenFound_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/v1/chat/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteConversation_WhenFound_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        var response = await _client.DeleteAsync($"/api/v1/chat/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

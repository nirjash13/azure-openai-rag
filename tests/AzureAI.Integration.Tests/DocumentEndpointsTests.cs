using System.Net;
using System.Net.Http.Headers;
using AzureAI.Core.Domain.Entities;
using AzureAI.Core.Domain.Enums;
using FluentAssertions;
using Moq;

namespace AzureAI.Integration.Tests;

public sealed class DocumentEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public DocumentEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDocuments_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/documents");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostDocument_WithTxtFile_ReturnsCreated()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("hello world"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "test.txt");

        var response = await _client.PostAsync("/api/v1/documents", content);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDocumentById_WhenNotFound_Returns404()
    {
        var id = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/v1/documents/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDocumentById_WhenFound_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var doc = new Document("found.txt", "text/plain", 100, DocumentType.Txt);

        _factory.DocumentRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        var response = await _client.GetAsync($"/api/v1/documents/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteDocument_WhenFound_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        var doc = new Document("todelete.txt", "text/plain", 50, DocumentType.Txt);

        _factory.DocumentRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        var response = await _client.DeleteAsync($"/api/v1/documents/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

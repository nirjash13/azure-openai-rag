using AzureAI.Core.Domain.Entities;
using AzureAI.Core.Domain.Enums;
using AzureAI.Core.Domain.ValueObjects;
using AzureAI.Core.Interfaces.Repositories;
using AzureAI.Core.Interfaces.Services;
using AzureAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace AzureAI.Integration.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IDocumentRepository> DocumentRepository { get; } = new();
    public Mock<IChunkRepository> ChunkRepository { get; } = new();
    public Mock<IConversationRepository> ConversationRepository { get; } = new();
    public Mock<IEmbeddingService> EmbeddingService { get; } = new();
    public Mock<ICompletionService> CompletionService { get; } = new();
    public Mock<IVectorSearchService> VectorSearchService { get; } = new();
    public Mock<ITextChunker> TextChunker { get; } = new();
    public Mock<IDocumentParser> DocumentParser { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureOpenAI:Endpoint"]            = "https://fake.openai.azure.com/",
                ["AzureOpenAI:ApiKey"]              = "fake-key",
                ["AzureOpenAI:ChatDeployment"]      = "gpt-4o",
                ["AzureOpenAI:EmbeddingDeployment"] = "text-embedding-3-large",
                ["AzureSearch:Endpoint"]            = "https://fake.search.windows.net",
                ["AzureSearch:ApiKey"]              = "fake-key",
                ["AzureSearch:IndexName"]           = "test-index",
                ["ConnectionStrings:Default"]       = "Host=localhost;Database=test;Username=test;Password=test"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the real DbContext registration
            services.RemoveAll<DbContextOptions<AzureAIDbContext>>();
            services.RemoveAll<AzureAIDbContext>();

            // Remove infrastructure service registrations
            services.RemoveAll<IDocumentRepository>();
            services.RemoveAll<IChunkRepository>();
            services.RemoveAll<IConversationRepository>();
            services.RemoveAll<IEmbeddingService>();
            services.RemoveAll<ICompletionService>();
            services.RemoveAll<IVectorSearchService>();
            services.RemoveAll<ITextChunker>();
            services.RemoveAll<IDocumentParser>();

            // Setup mocks
            SetupDocumentRepositoryMock();
            SetupConversationRepositoryMock();
            SetupEmbeddingServiceMock();
            SetupCompletionServiceMock();
            SetupVectorSearchServiceMock();
            SetupTextChunkerMock();
            SetupDocumentParserMock();

            // Register mocks
            services.AddSingleton(DocumentRepository.Object);
            services.AddSingleton(ChunkRepository.Object);
            services.AddSingleton(ConversationRepository.Object);
            services.AddSingleton(EmbeddingService.Object);
            services.AddSingleton(CompletionService.Object);
            services.AddSingleton(VectorSearchService.Object);
            services.AddSingleton(TextChunker.Object);
            services.AddSingleton(DocumentParser.Object);
        });
    }

    private void SetupDocumentRepositoryMock()
    {
        DocumentRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Document>)[]);
        DocumentRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);
        DocumentRepository
            .Setup(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        DocumentRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        DocumentRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupConversationRepositoryMock()
    {
        ConversationRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversationSession());
        ConversationRepository
            .Setup(r => r.GetRecentMessagesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ChatMessage>)[]);
        ConversationRepository
            .Setup(r => r.CreateAsync(It.IsAny<ConversationSession>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        ConversationRepository
            .Setup(r => r.AppendMessagesAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ChatMessage>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        ConversationRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupEmbeddingServiceMock()
    {
        var embedding = new EmbeddingVector([0.1f, 0.2f]);
        EmbeddingService
            .Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);
        EmbeddingService
            .Setup(e => e.GenerateBatchEmbeddingsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string> texts, CancellationToken _) =>
                (IReadOnlyList<EmbeddingVector>)texts.Select(_ => new EmbeddingVector([0.1f, 0.2f])).ToList());
    }

    private void SetupCompletionServiceMock()
    {
        var tokenUsage = new TokenUsage(10, 20, 0.01m);
        CompletionService
            .Setup(c => c.GenerateAsync(It.IsAny<IReadOnlyList<(ChatRole, string)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult("Test answer from mock.", tokenUsage));
    }

    private void SetupVectorSearchServiceMock()
    {
        VectorSearchService
            .Setup(v => v.SearchAsync(It.IsAny<EmbeddingVector>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SearchResult>)[]);
        VectorSearchService
            .Setup(v => v.IndexChunksAsync(It.IsAny<IReadOnlyList<DocumentChunk>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        VectorSearchService
            .Setup(v => v.RemoveDocumentChunksAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupTextChunkerMock()
    {
        var chunk = new TextChunk("sample text", 0, 0, 11);
        TextChunker
            .Setup(t => t.ChunkText(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns([chunk]);
    }

    private void SetupDocumentParserMock()
    {
        DocumentParser.Setup(p => p.SupportedType).Returns(DocumentType.Txt);
        DocumentParser
            .Setup(p => p.ExtractTextAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("sample text");
    }
}

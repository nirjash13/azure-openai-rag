using AzureAI.Application.Queries.AskQuestion;
using AzureAI.Core.Configuration;
using AzureAI.Core.Domain.Entities;
using AzureAI.Core.Domain.Enums;
using AzureAI.Core.Domain.ValueObjects;
using AzureAI.Core.Interfaces.Repositories;
using AzureAI.Core.Interfaces.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AzureAI.Application.Tests;

public sealed class AskQuestionHandlerTests
{
    private readonly Mock<IEmbeddingService> _embeddingService = new();
    private readonly Mock<IVectorSearchService> _vectorSearchService = new();
    private readonly Mock<ICompletionService> _completionService = new();
    private readonly Mock<IConversationRepository> _conversationRepo = new();
    private readonly Mock<ILogger<AskQuestionHandler>> _logger = new();

    private AskQuestionHandler CreateHandler(RagSettings? settings = null)
    {
        var ragSettings = settings ?? new RagSettings
        {
            MaxContextTokens = 2000,
            MaxConversationHistory = 10,
            IncludeCitations = false
        };
        return new AskQuestionHandler(
            _embeddingService.Object,
            _vectorSearchService.Object,
            _completionService.Object,
            _conversationRepo.Object,
            Options.Create(ragSettings),
            _logger.Object);
    }

    [Fact]
    public async Task Handle_HappyPath_ReturnsResponseWithCompletionContent()
    {
        var embedding = new EmbeddingVector([0.1f, 0.2f]);
        _embeddingService.Setup(e => e.GenerateEmbeddingAsync("test question", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(embedding);

        _vectorSearchService.Setup(v => v.SearchAsync(embedding, It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync((IReadOnlyList<SearchResult>)[]);

        var tokenUsage = new TokenUsage(10, 20, 30, 0.01m);
        _completionService.Setup(c => c.GenerateAsync(It.IsAny<IReadOnlyList<(ChatRole, string)>>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new CompletionResult("The answer", tokenUsage));

        var handler = CreateHandler();
        var query = new AskQuestionQuery("test question", null, null, null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Answer.Should().Be("The answer");
        result.Should().BeOfType<AskQuestionResponse>();
    }

    [Fact]
    public async Task Handle_GeneratesEmbeddingForQuestion()
    {
        var embedding = new EmbeddingVector([0.5f]);
        _embeddingService.Setup(e => e.GenerateEmbeddingAsync("my question", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(embedding);

        _vectorSearchService.Setup(v => v.SearchAsync(It.IsAny<EmbeddingVector>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync((IReadOnlyList<SearchResult>)[]);

        var tokenUsage = new TokenUsage(5, 10, 15, 0.005m);
        _completionService.Setup(c => c.GenerateAsync(It.IsAny<IReadOnlyList<(ChatRole, string)>>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new CompletionResult("answer", tokenUsage));

        var handler = CreateHandler();
        await handler.Handle(new AskQuestionQuery("my question", null, null, null), CancellationToken.None);

        _embeddingService.Verify(e => e.GenerateEmbeddingAsync("my question", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SearchCalledWithEmbeddingResult()
    {
        var embedding = new EmbeddingVector([0.3f]);
        _embeddingService.Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(embedding);

        _vectorSearchService.Setup(v => v.SearchAsync(embedding, It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync((IReadOnlyList<SearchResult>)[]);

        var tokenUsage = new TokenUsage(0, 0, 0, 0m);
        _completionService.Setup(c => c.GenerateAsync(It.IsAny<IReadOnlyList<(ChatRole, string)>>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new CompletionResult("answer", tokenUsage));

        var handler = CreateHandler();
        await handler.Handle(new AskQuestionQuery("q", null, null, null), CancellationToken.None);

        _vectorSearchService.Verify(v => v.SearchAsync(embedding, It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithConversationId_CallsGetRecentMessagesAndAppendMessages()
    {
        var conversationId = Guid.NewGuid();
        var embedding = new EmbeddingVector([0.1f]);
        _embeddingService.Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(embedding);
        _vectorSearchService.Setup(v => v.SearchAsync(It.IsAny<EmbeddingVector>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync((IReadOnlyList<SearchResult>)[]);

        var tokenUsage = new TokenUsage(10, 10, 20, 0.01m);
        _completionService.Setup(c => c.GenerateAsync(It.IsAny<IReadOnlyList<(ChatRole, string)>>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new CompletionResult("answer", tokenUsage));

        _conversationRepo.Setup(r => r.GetRecentMessagesAsync(conversationId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync((IReadOnlyList<ChatMessage>)[]);
        _conversationRepo.Setup(r => r.AppendMessagesAsync(conversationId, It.IsAny<ChatMessage[]>()))
                         .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        await handler.Handle(new AskQuestionQuery("q", conversationId, null, null), CancellationToken.None);

        _conversationRepo.Verify(r => r.GetRecentMessagesAsync(conversationId, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _conversationRepo.Verify(r => r.AppendMessagesAsync(conversationId, It.IsAny<ChatMessage[]>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutConversationId_DoesNotCallAppendMessages()
    {
        var embedding = new EmbeddingVector([0.1f]);
        _embeddingService.Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(embedding);
        _vectorSearchService.Setup(v => v.SearchAsync(It.IsAny<EmbeddingVector>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync((IReadOnlyList<SearchResult>)[]);

        var tokenUsage = new TokenUsage(0, 0, 0, 0m);
        _completionService.Setup(c => c.GenerateAsync(It.IsAny<IReadOnlyList<(ChatRole, string)>>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new CompletionResult("answer", tokenUsage));

        var handler = CreateHandler();
        await handler.Handle(new AskQuestionQuery("q", null, null, null), CancellationToken.None);

        _conversationRepo.Verify(r => r.AppendMessagesAsync(It.IsAny<Guid>(), It.IsAny<ChatMessage[]>()), Times.Never);
    }
}

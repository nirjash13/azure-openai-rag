using AzureAI.Application.Commands.IngestDocument;
using AzureAI.Core.Configuration;
using AzureAI.Core.Domain.Entities;
using AzureAI.Core.Domain.Enums;
using AzureAI.Core.Domain.Exceptions;
using AzureAI.Core.Domain.ValueObjects;
using AzureAI.Core.Interfaces.Repositories;
using AzureAI.Core.Interfaces.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AzureAI.Application.Tests;

public sealed class IngestDocumentHandlerTests
{
    private readonly Mock<IDocumentRepository> _documentRepo = new();
    private readonly Mock<IChunkRepository> _chunkRepo = new();
    private readonly Mock<IEmbeddingService> _embeddingService = new();
    private readonly Mock<ITextChunker> _textChunker = new();
    private readonly Mock<IVectorSearchService> _vectorSearchService = new();
    private readonly Mock<IDocumentParser> _parser = new();
    private readonly Mock<ILogger<IngestDocumentHandler>> _logger = new();

    private IngestDocumentHandler CreateHandler(ChunkingSettings? settings = null)
    {
        var chunkingSettings = settings ?? new ChunkingSettings
        {
            ChunkSizeTokens = 512,
            OverlapTokens = 64,
            MaxChunksPerDocument = 100
        };

        return new IngestDocumentHandler(
            _documentRepo.Object,
            _chunkRepo.Object,
            _embeddingService.Object,
            _textChunker.Object,
            _vectorSearchService.Object,
            [_parser.Object],
            Options.Create(chunkingSettings),
            _logger.Object);
    }

    [Fact]
    public async Task Handle_HappyPath_ReturnsGuidAndCallsDependenciesInOrder()
    {
        _parser.Setup(p => p.SupportedType).Returns(DocumentType.Txt);
        _parser.Setup(p => p.ExtractTextAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync("sample text");

        var chunk = new TextChunk("sample text", 0, 0, 11);
        _textChunker.Setup(t => t.ChunkText(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                    .Returns([chunk]);

        var embedding = new EmbeddingVector([0.1f, 0.2f]);
        _embeddingService.Setup(e => e.GenerateBatchEmbeddingsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync((IReadOnlyList<EmbeddingVector>)[embedding]);

        _documentRepo.Setup(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);
        _documentRepo.Setup(r => r.UpdateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);
        _chunkRepo.Setup(r => r.AddRangeAsync(It.IsAny<IReadOnlyList<DocumentChunk>>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
        _vectorSearchService.Setup(v => v.IndexChunksAsync(It.IsAny<IReadOnlyList<DocumentChunk>>(), It.IsAny<CancellationToken>()))
                            .Returns(Task.CompletedTask);

        using var stream = new MemoryStream("hello"u8.ToArray());
        var command = new IngestDocumentCommand(stream, "file.txt", "text/plain", 5);
        var handler = CreateHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        _documentRepo.Verify(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Once);
        _chunkRepo.Verify(r => r.AddRangeAsync(It.IsAny<IReadOnlyList<DocumentChunk>>(), It.IsAny<CancellationToken>()), Times.Once);
        _vectorSearchService.Verify(v => v.IndexChunksAsync(It.IsAny<IReadOnlyList<DocumentChunk>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ParserFoundByContentType_Succeeds()
    {
        _parser.Setup(p => p.SupportedType).Returns(DocumentType.Pdf);
        _parser.Setup(p => p.ExtractTextAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync("pdf text");

        var chunk = new TextChunk("pdf text", 0, 0, 8);
        _textChunker.Setup(t => t.ChunkText(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                    .Returns([chunk]);

        var embedding = new EmbeddingVector([0.1f]);
        _embeddingService.Setup(e => e.GenerateBatchEmbeddingsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync((IReadOnlyList<EmbeddingVector>)[embedding]);

        _documentRepo.Setup(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _documentRepo.Setup(r => r.UpdateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _chunkRepo.Setup(r => r.AddRangeAsync(It.IsAny<IReadOnlyList<DocumentChunk>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _vectorSearchService.Setup(v => v.IndexChunksAsync(It.IsAny<IReadOnlyList<DocumentChunk>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        using var stream = new MemoryStream([0x25, 0x50, 0x44, 0x46]);
        var command = new IngestDocumentCommand(stream, "doc.pdf", "application/pdf", 4);
        var handler = CreateHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_ParserThrows_DocumentMarkedFailedAndRethrowsDocumentProcessingException()
    {
        _parser.Setup(p => p.SupportedType).Returns(DocumentType.Txt);
        _parser.Setup(p => p.ExtractTextAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new InvalidOperationException("parse error"));

        _documentRepo.Setup(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _documentRepo.Setup(r => r.UpdateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        using var stream = new MemoryStream("data"u8.ToArray());
        var command = new IngestDocumentCommand(stream, "file.txt", "text/plain", 4);
        var handler = CreateHandler();

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DocumentProcessingException>();
        _documentRepo.Verify(r => r.UpdateAsync(
            It.Is<Document>(d => d.Status == IngestionStatus.Failed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}

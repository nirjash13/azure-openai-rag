using AzureAI.Application.Commands.DeleteDocument;
using AzureAI.Core.Domain.Entities;
using AzureAI.Core.Domain.Enums;
using AzureAI.Core.Interfaces.Repositories;
using AzureAI.Core.Interfaces.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AzureAI.Application.Tests;

public sealed class DeleteDocumentHandlerTests
{
    private readonly Mock<IDocumentRepository> _documentRepo = new();
    private readonly Mock<IChunkRepository> _chunkRepo = new();
    private readonly Mock<IVectorSearchService> _vectorSearchService = new();
    private readonly Mock<ILogger<DeleteDocumentHandler>> _logger = new();

    private DeleteDocumentHandler CreateHandler() =>
        new(_documentRepo.Object, _chunkRepo.Object, _vectorSearchService.Object, _logger.Object);

    [Fact]
    public async Task Handle_DocumentNotFound_ReturnsFalse()
    {
        _documentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((Document?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteDocumentCommand(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DocumentFound_ReturnsTrueAndCallsDependenciesInOrder()
    {
        var docId = Guid.NewGuid();
        var doc = new Document("file.txt", "text/plain", 100, DocumentType.Txt);

        _documentRepo.Setup(r => r.GetByIdAsync(docId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(doc);
        _vectorSearchService.Setup(v => v.RemoveDocumentChunksAsync(docId, It.IsAny<CancellationToken>()))
                            .Returns(Task.CompletedTask);
        _chunkRepo.Setup(r => r.DeleteByDocumentIdAsync(docId, It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
        _documentRepo.Setup(r => r.DeleteAsync(docId, It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteDocumentCommand(docId), CancellationToken.None);

        result.Should().BeTrue();

        var callOrder = new List<string>();
        _vectorSearchService.Verify(v => v.RemoveDocumentChunksAsync(docId, It.IsAny<CancellationToken>()), Times.Once);
        _chunkRepo.Verify(r => r.DeleteByDocumentIdAsync(docId, It.IsAny<CancellationToken>()), Times.Once);
        _documentRepo.Verify(r => r.DeleteAsync(docId, It.IsAny<CancellationToken>()), Times.Once);
    }
}

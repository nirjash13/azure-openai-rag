using AzureAI.Core.Domain.Entities;
using AzureAI.Core.Domain.Enums;
using AzureAI.Core.Domain.Exceptions;
using FluentAssertions;

namespace AzureAI.Core.Tests;

public sealed class DocumentTests
{
    [Fact]
    public void Constructor_SetsInitialStatusToPending()
    {
        var doc = new Document("file.txt", "text/plain", 100, DocumentType.Txt);
        doc.Status.Should().Be(IngestionStatus.Pending);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsOnNullOrEmptyFileName(string? fileName)
    {
        var act = () => new Document(fileName!, "text/plain", 100, DocumentType.Txt);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ThrowsOnNegativeFileSize()
    {
        var act = () => new Document("file.txt", "text/plain", -1, DocumentType.Txt);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void MarkProcessing_PendingToProcesing_Succeeds()
    {
        var doc = new Document("file.txt", "text/plain", 100, DocumentType.Txt);
        doc.MarkProcessing();
        doc.Status.Should().Be(IngestionStatus.Processing);
    }

    [Fact]
    public void MarkProcessing_ThrowsWhenAlreadyProcessing()
    {
        var doc = new Document("file.txt", "text/plain", 100, DocumentType.Txt);
        doc.MarkProcessing();

        var act = () => doc.MarkProcessing();
        act.Should().Throw<DocumentProcessingException>();
    }

    [Fact]
    public void MarkCompleted_ProcessingToCompleted_SetsChunkCountAndCompletedAt()
    {
        var doc = new Document("file.txt", "text/plain", 100, DocumentType.Txt);
        doc.MarkProcessing();
        doc.MarkCompleted(5);

        doc.Status.Should().Be(IngestionStatus.Completed);
        doc.ChunkCount.Should().Be(5);
        doc.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkCompleted_ThrowsWhenStatusIsNotProcessing()
    {
        var doc = new Document("file.txt", "text/plain", 100, DocumentType.Txt);

        var act = () => doc.MarkCompleted(5);
        act.Should().Throw<DocumentProcessingException>();
    }

    [Fact]
    public void MarkCompleted_ThrowsOnNegativeChunkCount()
    {
        var doc = new Document("file.txt", "text/plain", 100, DocumentType.Txt);
        doc.MarkProcessing();

        var act = () => doc.MarkCompleted(-1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void MarkFailed_SetsStatusFailedAndFailureReason()
    {
        var doc = new Document("file.txt", "text/plain", 100, DocumentType.Txt);
        doc.MarkProcessing();
        doc.MarkFailed("something broke");

        doc.Status.Should().Be(IngestionStatus.Failed);
        doc.FailureReason.Should().Be("something broke");
    }

    [Fact]
    public void MarkFailed_ThrowsWhenStatusIsCompleted()
    {
        var doc = new Document("file.txt", "text/plain", 100, DocumentType.Txt);
        doc.MarkProcessing();
        doc.MarkCompleted(3);

        var act = () => doc.MarkFailed("reason");
        act.Should().Throw<DocumentProcessingException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkFailed_ThrowsOnNullOrEmptyReason(string? reason)
    {
        var doc = new Document("file.txt", "text/plain", 100, DocumentType.Txt);

        var act = () => doc.MarkFailed(reason!);
        act.Should().Throw<ArgumentException>();
    }
}

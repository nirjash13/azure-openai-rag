using AzureAI.Application.Behaviors;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace AzureAI.Application.Tests;

public sealed class PerformanceBehaviorTests
{
    public sealed record FakeRequest : IRequest<FakeResponse>;
    public sealed record FakeResponse;

    [Fact]
    public async Task Handle_FastRequest_CompletesSuccessfullyWithoutWarning()
    {
        var loggerMock = new Mock<ILogger<PerformanceBehavior<FakeRequest, FakeResponse>>>();
        var behavior = new PerformanceBehavior<FakeRequest, FakeResponse>(loggerMock.Object);
        var expected = new FakeResponse();

        var result = await behavior.Handle(
            new FakeRequest(),
            _ => Task.FromResult(expected),
            CancellationToken.None);

        result.Should().BeSameAs(expected);
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_SlowRequest_LogsWarning()
    {
        var loggerMock = new Mock<ILogger<PerformanceBehavior<FakeRequest, FakeResponse>>>();
        var behavior = new PerformanceBehavior<FakeRequest, FakeResponse>(loggerMock.Object);
        var expected = new FakeResponse();

        await behavior.Handle(
            new FakeRequest(),
            async _ =>
            {
                await Task.Delay(600);
                return expected;
            },
            CancellationToken.None);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

using AzureAI.Core.Domain.Entities;
using AzureAI.Core.Domain.Enums;
using FluentAssertions;

namespace AzureAI.Core.Tests;

public sealed class ConversationSessionTests
{
    [Fact]
    public void Constructor_SetsNonEmptyId_AndTimestamps()
    {
        var session = new ConversationSession();

        session.Id.Should().NotBe(Guid.Empty);
        session.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        session.LastMessageAt.Should().Be(session.CreatedAt);
    }

    [Fact]
    public void Constructor_StoresOptionalTitle()
    {
        var session = new ConversationSession("My Chat");
        session.Title.Should().Be("My Chat");
    }

    [Fact]
    public void AddMessage_ReturnsChatMessageWithCorrectRoleAndContent()
    {
        var session = new ConversationSession();
        var msg = session.AddMessage(ChatRole.User, "Hello");

        msg.Role.Should().Be(ChatRole.User);
        msg.Content.Should().Be("Hello");
    }

    [Fact]
    public void AddMessage_AppendsToMessagesCollection()
    {
        var session = new ConversationSession();
        session.AddMessage(ChatRole.User, "First");
        session.AddMessage(ChatRole.Assistant, "Second");

        session.Messages.Should().HaveCount(2);
    }

    [Fact]
    public void AddMessage_UpdatesLastMessageAt()
    {
        var session = new ConversationSession();
        var before = session.LastMessageAt;

        session.AddMessage(ChatRole.User, "Hello");

        session.LastMessageAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void SetTitle_UpdatesTitle()
    {
        var session = new ConversationSession();
        session.SetTitle("New Title");
        session.Title.Should().Be("New Title");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetTitle_ThrowsOnNullOrEmpty(string? title)
    {
        var session = new ConversationSession();
        var act = () => session.SetTitle(title!);
        act.Should().Throw<ArgumentException>();
    }
}

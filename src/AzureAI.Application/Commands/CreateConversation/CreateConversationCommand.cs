using MediatR;

namespace AzureAI.Application.Commands.CreateConversation;

/// <summary>Command that creates a new conversation session and returns its identifier.</summary>
public sealed record CreateConversationCommand(string? Title) : IRequest<Guid>;

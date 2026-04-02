using MediatR;

namespace AzureAI.Application.Commands.DeleteConversation;

/// <summary>Removes a conversation session and all its messages.</summary>
public sealed record DeleteConversationCommand(Guid ConversationId) : IRequest<bool>;

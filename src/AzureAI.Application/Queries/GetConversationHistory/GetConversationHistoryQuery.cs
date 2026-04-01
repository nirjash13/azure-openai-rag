using MediatR;

namespace AzureAI.Application.Queries.GetConversationHistory;

/// <summary>Query that returns the full message history of a conversation session.</summary>
public sealed record GetConversationHistoryQuery(Guid ConversationId) : IRequest<ConversationHistoryResponse>;

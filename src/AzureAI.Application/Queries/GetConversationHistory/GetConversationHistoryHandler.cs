using AzureAI.Core.Interfaces.Repositories;
using MediatR;

namespace AzureAI.Application.Queries.GetConversationHistory;

/// <summary>Fetches a conversation session and maps it to a history response.</summary>
public sealed class GetConversationHistoryHandler
    : IRequestHandler<GetConversationHistoryQuery, ConversationHistoryResponse>
{
    private readonly IConversationRepository _conversationRepository;

    /// <summary>Initializes a new <see cref="GetConversationHistoryHandler"/>.</summary>
    public GetConversationHistoryHandler(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    /// <inheritdoc />
    public async Task<ConversationHistoryResponse> Handle(
        GetConversationHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var session = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (session is null)
            throw new KeyNotFoundException($"Conversation '{request.ConversationId}' not found.");

        var messages = session.Messages
            .Select(m => new ConversationMessageDto(m.Id, m.Role.ToString(), m.Content, m.CreatedAt))
            .ToList();

        return new ConversationHistoryResponse(
            session.Id,
            session.Title,
            session.CreatedAt,
            session.LastMessageAt,
            messages);
    }
}

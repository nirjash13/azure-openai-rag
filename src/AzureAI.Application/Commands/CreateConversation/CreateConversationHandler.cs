using AzureAI.Core.Domain.Entities;
using AzureAI.Core.Interfaces.Repositories;
using MediatR;

namespace AzureAI.Application.Commands.CreateConversation;

/// <summary>Creates a new <see cref="ConversationSession"/> and persists it.</summary>
public sealed class CreateConversationHandler : IRequestHandler<CreateConversationCommand, Guid>
{
    private readonly IConversationRepository _conversationRepository;

    /// <summary>Initializes a new <see cref="CreateConversationHandler"/>.</summary>
    public CreateConversationHandler(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    /// <inheritdoc />
    public async Task<Guid> Handle(CreateConversationCommand request, CancellationToken cancellationToken)
    {
        var session = new ConversationSession(request.Title);
        await _conversationRepository.CreateAsync(session, cancellationToken);
        return session.Id;
    }
}

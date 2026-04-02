using AzureAI.Core.Interfaces.Repositories;
using MediatR;

namespace AzureAI.Application.Commands.DeleteConversation;

internal sealed class DeleteConversationHandler : IRequestHandler<DeleteConversationCommand, bool>
{
    private readonly IConversationRepository _conversations;

    public DeleteConversationHandler(IConversationRepository conversations)
    {
        _conversations = conversations;
    }

    public async Task<bool> Handle(DeleteConversationCommand request, CancellationToken cancellationToken)
    {
        var session = await _conversations.GetByIdAsync(request.ConversationId, cancellationToken);
        if (session is null)
            return false;

        await _conversations.DeleteAsync(request.ConversationId, cancellationToken);
        return true;
    }
}

using MediatR;

namespace AzureAI.Application.Queries.AskQuestion;

/// <summary>Query that runs the RAG pipeline for a user question and returns a grounded answer.</summary>
public sealed record AskQuestionQuery(
    string Question,
    Guid? ConversationId,
    int? TopK,
    bool? IncludeCitations) : IRequest<AskQuestionResponse>;

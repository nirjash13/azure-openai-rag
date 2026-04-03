using System.Text.Json;
using AzureAI.Application.Queries.SearchDocuments;
using AzureAI.FunctionCalling.Abstractions;
using MediatR;

namespace AzureAI.FunctionCalling.Tools;

[ToolFunction("search_documents", "Searches the document store for relevant content")]
public sealed class SearchDocumentsTool : IToolDefinition
{
    private readonly ISender _sender;

    public SearchDocumentsTool(ISender sender) => _sender = sender;

    public string Name => "search_documents";
    public string Description => "Searches the document store for relevant content";
    public string ParameterSchema => """
        {
            "type": "object",
            "properties": {
                "query": {
                    "type": "string",
                    "description": "The search query text"
                },
                "top_k": {
                    "type": "integer",
                    "description": "Number of results to return (optional)"
                }
            },
            "required": ["query"]
        }
        """;

    public async Task<string> ExecuteAsync(string argumentsJson, ToolExecutionContext context, CancellationToken cancellationToken)
    {
        string query = string.Empty;
        int?   topK  = null;

        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("query", out var q))
                query = q.GetString() ?? string.Empty;

            if (root.TryGetProperty("top_k", out var k) && k.ValueKind == JsonValueKind.Number)
                topK = k.GetInt32();
        }
        catch (JsonException)
        {
            return """{"error":"Invalid arguments JSON"}""";
        }

        if (string.IsNullOrWhiteSpace(query))
            return """{"error":"query is required"}""";

        var results = await _sender.Send(new SemanticSearchQuery(query, topK, null), cancellationToken);
        return JsonSerializer.Serialize(results);
    }
}

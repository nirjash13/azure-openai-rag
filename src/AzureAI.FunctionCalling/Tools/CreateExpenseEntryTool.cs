using System.Text.Json;
using AzureAI.FunctionCalling.Abstractions;

namespace AzureAI.FunctionCalling.Tools;

[ToolFunction("create_expense_entry", "Creates a new expense entry in the system")]
public sealed class CreateExpenseEntryTool : IToolDefinition
{
    public string Name => "create_expense_entry";
    public string Description => "Creates a new expense entry in the system";
    public string ParameterSchema => """
        {
            "type": "object",
            "properties": {
                "amount": {
                    "type": "number",
                    "description": "The expense amount"
                },
                "category": {
                    "type": "string",
                    "description": "The expense category"
                },
                "description": {
                    "type": "string",
                    "description": "A short description of the expense"
                },
                "date": {
                    "type": "string",
                    "description": "ISO 8601 date string, e.g. '2024-03-15'"
                }
            },
            "required": ["amount", "category", "description", "date"]
        }
        """;

    public Task<string> ExecuteAsync(string argumentsJson, ToolExecutionContext context, CancellationToken cancellationToken)
    {
        if (!context.IsDestructiveConfirmed)
        {
            return Task.FromResult("""{"confirmationRequired":true,"message":"This action will create a new expense entry. Please confirm to proceed."}""");
        }

        decimal amount      = 0;
        string  category    = string.Empty;
        string  description = string.Empty;
        string  date        = string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("amount", out var a) && a.ValueKind == JsonValueKind.Number)
                amount = a.GetDecimal();

            if (root.TryGetProperty("category", out var cat))
                category = cat.GetString() ?? string.Empty;

            if (root.TryGetProperty("description", out var desc))
                description = desc.GetString() ?? string.Empty;

            if (root.TryGetProperty("date", out var d))
                date = d.GetString() ?? string.Empty;
        }
        catch (JsonException)
        {
            return Task.FromResult("""{"error":"Invalid arguments JSON"}""");
        }

        var result = JsonSerializer.Serialize(new
        {
            id          = Guid.NewGuid(),
            amount,
            category,
            description,
            date,
            status      = "created",
            createdAt   = DateTime.UtcNow.ToString("o")
        });

        return Task.FromResult(result);
    }
}

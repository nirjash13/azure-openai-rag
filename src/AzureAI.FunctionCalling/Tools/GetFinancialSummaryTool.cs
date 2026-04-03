using System.Text.Json;
using AzureAI.FunctionCalling.Abstractions;

namespace AzureAI.FunctionCalling.Tools;

[ToolFunction("get_financial_summary", "Returns a financial summary for a given period and category")]
public sealed class GetFinancialSummaryTool : IToolDefinition
{
    public string Name => "get_financial_summary";
    public string Description => "Returns a financial summary for a given period and category";
    public string ParameterSchema => """
        {
            "type": "object",
            "properties": {
                "period": {
                    "type": "string",
                    "description": "The reporting period, e.g. '2024-Q1'"
                },
                "category": {
                    "type": "string",
                    "description": "Optional expense or revenue category filter"
                }
            },
            "required": ["period"]
        }
        """;

    public Task<string> ExecuteAsync(string argumentsJson, ToolExecutionContext context, CancellationToken cancellationToken)
    {
        string period   = "unknown";
        string category = "all";

        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("period", out var p))
                period = p.GetString() ?? period;

            if (root.TryGetProperty("category", out var c))
                category = c.GetString() ?? category;
        }
        catch (JsonException)
        {
            return Task.FromResult("""{"error":"Invalid arguments JSON"}""");
        }

        var result = JsonSerializer.Serialize(new
        {
            period,
            category,
            totalRevenue    = 1_250_000.00m,
            totalExpenses   = 875_400.00m,
            netIncome       = 374_600.00m,
            currency        = "USD",
            generatedAt     = DateTime.UtcNow.ToString("o")
        });

        return Task.FromResult(result);
    }
}

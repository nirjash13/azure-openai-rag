using System.Text.Json;
using AzureAI.FunctionCalling.Abstractions;

namespace AzureAI.FunctionCalling.Tools;

[ToolFunction("get_weather", "Returns current weather for a given location")]
public sealed class GetWeatherTool : IToolDefinition
{
    public string Name => "get_weather";
    public string Description => "Returns current weather for a given location";
    public string ParameterSchema => """
        {
            "type": "object",
            "properties": {
                "location": {
                    "type": "string",
                    "description": "The city or location name"
                }
            },
            "required": ["location"]
        }
        """;

    public Task<string> ExecuteAsync(string argumentsJson, ToolExecutionContext context, CancellationToken cancellationToken)
    {
        string location = "unknown";

        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("location", out var loc))
                location = loc.GetString() ?? location;
        }
        catch (JsonException)
        {
            return Task.FromResult("""{"error":"Invalid arguments JSON"}""");
        }

        var result = JsonSerializer.Serialize(new
        {
            location,
            temperatureCelsius  = 18.5,
            condition           = "Partly cloudy",
            humidity            = 62,
            windSpeedKph        = 14,
            observedAt          = DateTime.UtcNow.ToString("o")
        });

        return Task.FromResult(result);
    }
}

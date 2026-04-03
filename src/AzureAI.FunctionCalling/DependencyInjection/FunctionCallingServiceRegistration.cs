using AzureAI.FunctionCalling.Tools;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AzureAI.FunctionCalling.DependencyInjection;

public static class FunctionCallingServiceRegistration
{
    public static IServiceCollection AddFunctionCalling(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<ToolRegistry>(sp =>
        {
            var registry = new ToolRegistry();
            var sender   = sp.GetRequiredService<ISender>();

            registry.Register(new GetFinancialSummaryTool());
            registry.Register(new SearchDocumentsTool(sender));
            registry.Register(new CreateExpenseEntryTool());
            registry.Register(new GetWeatherTool());

            return registry;
        });

        services.AddScoped<FunctionCallingOrchestrator>();

        return services;
    }
}

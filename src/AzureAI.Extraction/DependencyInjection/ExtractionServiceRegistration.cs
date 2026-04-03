using AzureAI.Extraction.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AzureAI.Extraction.DependencyInjection;

public static class ExtractionServiceRegistration
{
    public static IServiceCollection AddExtraction(this IServiceCollection services)
    {
        services.AddScoped<InvoiceExtractionService>();
        services.AddScoped<ReceiptExtractionService>();
        services.AddSingleton<ExtractionValidationService>();
        services.AddSingleton<AccountMappingService>();
        return services;
    }
}

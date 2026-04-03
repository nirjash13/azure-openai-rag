using AzureAI.Api.Endpoints;
using AzureAI.Api.HealthChecks;
using AzureAI.Api.Middleware;
using AzureAI.Application.DependencyInjection;
using AzureAI.Extraction.DependencyInjection;
using AzureAI.FunctionCalling.DependencyInjection;
using AzureAI.Infrastructure.DependencyInjection;
using AzureAI.Infrastructure.Persistence;
using AzureAI.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddFunctionCalling(builder.Configuration);
    builder.Services.AddExtraction();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(o =>
        o.SwaggerDoc("v1", new() { Title = "Azure OpenAI RAG API", Version = "v1" }));

    builder.Services.AddCors(o =>
        o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

    builder.Services.AddHttpClient();

    var pgConnection = builder.Configuration.GetConnectionString("Default") ?? string.Empty;
    builder.Services.AddHealthChecks()
        .AddNpgSql(pgConnection, name: "postgres", tags: ["db"])
        .AddCheck<AzureOpenAIHealthCheck>("azure-openai", tags: ["azure"])
        .AddCheck<AzureSearchHealthCheck>("azure-search", tags: ["azure"]);

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.ConfigureHttpJsonOptions(o =>
        o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

    var app = builder.Build();

    // Apply pending EF Core migrations automatically on startup.
    if (!app.Environment.IsEnvironment("Testing"))
    {
        using var dbScope = app.Services.CreateScope();
        var db = dbScope.ServiceProvider.GetRequiredService<AzureAIDbContext>();
        try
        {
            await db.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Database migration failed — app will start anyway");
        }
    }

    // Ensure the Azure AI Search index exists with the correct schema.
    // Vector dimension 3072 matches text-embedding-3-large; update if using a different model.
    if (!app.Environment.IsEnvironment("Testing"))
    {
        using var scope = app.Services.CreateScope();
        var indexManager = scope.ServiceProvider.GetRequiredService<SearchIndexManager>();
        try
        {
            await indexManager.EnsureIndexExistsAsync(vectorDimension: 3072);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to ensure search index exists — app will start anyway");
        }
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "Azure OpenAI RAG v1"));
    }

    app.UseExceptionHandler();
    app.UseCors();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (ctx, report) =>
        {
            ctx.Response.ContentType = "application/json";
            var result = new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description
                })
            };
            await ctx.Response.WriteAsJsonAsync(result);
        }
    });

    app.MapDocumentEndpoints();
    app.MapChatEndpoints();
    app.MapSearchEndpoints();
    app.MapAgentEndpoints();
    app.MapExtractionEndpoints();
    app.MapAnalyticsEndpoints();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application startup failed");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Stub required by WebApplicationFactory in integration tests
public partial class Program { }

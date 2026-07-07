using System.Threading.RateLimiting;
using ChatbotService.Api.Options;
using ChatbotService.Api.Providers;
using ChatbotService.BLL.Abstractions;
using ChatbotService.BLL.Observability;
using ChatbotService.BLL.Options;
using ChatbotService.BLL.Security;
using ChatbotService.BLL.Services;
using ChatbotService.BLL.TextExtraction;
using ChatbotService.DAL;
using OpenTelemetry.Metrics;
using Shared.Semantic;

namespace ChatbotService.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddChatbotApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenAiOptions>(configuration.GetSection("OpenAi"));
        services.Configure<EmbeddingOptions>(configuration.GetSection("Embedding"));
        services.Configure<PromptOptions>(configuration.GetSection("Prompt"));
        services.Configure<RagOptions>(configuration.GetSection("Rag"));
        services.Configure<ChatbotRuntimeOptions>(configuration.GetSection("Chatbot"));

        services.AddChatbotDal(configuration);
        services.AddHealthChecks();
        services.AddSingleton<ChatbotMetrics>();
        services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddMeter(ChatbotMetrics.MeterName)
                .AddOtlpExporter());
        services.AddRateLimiter(options =>
        {
            options.AddPolicy("chatbot", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
        });

        services.AddScoped<IChatbotOrchestrator, ChatbotOrchestrator>();
        services.AddScoped<IRagRetriever, RagRetriever>();
        services.AddScoped<IDocumentIngestionService, DocumentIngestionService>();
        services.AddScoped<IDocumentQueryService, DocumentQueryService>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddScoped<IConversationQueryService, ConversationQueryService>();
        services.AddScoped<ISemanticSearchService, SemanticSearchService>();
        services.AddScoped<IConversationMemoryService, ConversationMemoryService>();
        services.AddScoped<IConversationSummaryService, ConversationSummaryService>();
        services.AddScoped<ITextExtractionService, TextExtractionService>();
        services.AddScoped<EmbeddingService>();
        services.AddSingleton<ITextChunker, DefaultTextChunker>();
        services.AddSingleton<ChunkingService>();
        services.AddSingleton<PromptBuilder>();
        services.AddSingleton<AnimalSafetyClassifier>();
        services.AddSingleton<AnimalSafetyService>();
        services.AddSingleton<CitationBuilder>();
        services.AddSingleton<CitationService>();
        services.AddSingleton<TokenBudgetManager>();
        services.AddSingleton<ChunkScoringService>();
        services.AddSingleton<DocumentRankingService>();
        services.AddSingleton<HallucinationGuard>();
        services.AddSingleton<InputSanitizer>();
        services.AddSingleton<PromptInjectionGuard>();

        services.AddHttpClient<OpenAiProvider>((provider, client) =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenAiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        });
        services.AddScoped<ILLMProvider>(provider => provider.GetRequiredService<OpenAiProvider>());
        services.AddScoped<IEmbeddingProvider>(provider => new CachedEmbeddingProvider(
            provider.GetRequiredService<OpenAiProvider>(),
            provider.GetRequiredService<ChatbotService.DAL.Abstractions.IEmbeddingCacheRepository>(),
            provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<EmbeddingOptions>>(),
            provider.GetRequiredService<ChatbotMetrics>()));

        return services;
    }
}

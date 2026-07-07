using ChatbotService.Api.Options;
using ChatbotService.Api.Providers;
using ChatbotService.BLL.Abstractions;
using ChatbotService.BLL.Services;
using ChatbotService.DAL;
using Shared.Semantic;

namespace ChatbotService.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddChatbotApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenAiOptions>(configuration.GetSection("OpenAi"));
        services.Configure<ChatbotOptions>(configuration.GetSection("Chatbot"));

        services.AddChatbotDal(configuration);

        services.AddScoped<IChatbotOrchestrator, ChatbotOrchestrator>();
        services.AddScoped<IRagRetriever, RagRetriever>();
        services.AddScoped<IDocumentIngestionService, DocumentIngestionService>();
        services.AddSingleton<ITextChunker, DefaultTextChunker>();
        services.AddSingleton<PromptBuilder>();
        services.AddSingleton<AnimalSafetyClassifier>();
        services.AddSingleton<CitationBuilder>();

        services.AddHttpClient<OpenAiProvider>((provider, client) =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenAiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        });
        services.AddScoped<IEmbeddingProvider>(provider => provider.GetRequiredService<OpenAiProvider>());
        services.AddScoped<ILLMProvider>(provider => provider.GetRequiredService<OpenAiProvider>());

        return services;
    }
}

using ChatbotService.DAL.Abstractions;
using ChatbotService.DAL.Db;
using ChatbotService.DAL.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Persistence.Extensions;

namespace ChatbotService.DAL;

public static class DependencyInjection
{
    public static IServiceCollection AddChatbotDal(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPostgresPersistence(configuration);
        services.AddSingleton<ChatbotDbConnectionFactory>();
        services.AddScoped<IChatConversationRepository, ChatConversationRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
        services.AddScoped<IKnowledgeDocumentRepository, KnowledgeDocumentRepository>();
        services.AddScoped<IVectorSearchRepository, PgVectorSearchRepository>();

        return services;
    }
}

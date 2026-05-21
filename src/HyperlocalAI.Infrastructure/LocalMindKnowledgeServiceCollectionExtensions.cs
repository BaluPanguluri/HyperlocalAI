using HyperlocalAI.Application.Knowledge;
using HyperlocalAI.Infrastructure.Knowledge;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HyperlocalAI.Infrastructure;

public static class LocalMindKnowledgeServiceCollectionExtensions
{
    public static IServiceCollection AddLocalMindKnowledge(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KnowledgeStorageOptions>(configuration.GetSection(KnowledgeStorageOptions.SectionName));
        services.AddMemoryCache();
        services.AddSingleton<TenantKnowledgeService>();
        services.AddSingleton<ITenantKnowledgeProvider>(sp => sp.GetRequiredService<TenantKnowledgeService>());
        services.AddSingleton<IKnowledgeDocumentAdmin>(sp => sp.GetRequiredService<TenantKnowledgeService>());
        return services;
    }
}

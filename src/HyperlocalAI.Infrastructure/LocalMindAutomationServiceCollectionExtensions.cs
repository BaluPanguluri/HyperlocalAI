using HyperlocalAI.Application.Automation;
using HyperlocalAI.Infrastructure.Automation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HyperlocalAI.Infrastructure;

public static class LocalMindAutomationServiceCollectionExtensions
{
    /// <summary>WhatsApp Cloud orchestration. Requires <see cref="Persistence.EnterprisePersistenceExtensions.AddLocalMindEnterprisePersistence"/> for bookings, CRM, and dashboard.</summary>
    public static IServiceCollection AddLocalMindAutomation(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WhatsAppOptions>(configuration.GetSection(WhatsAppOptions.SectionName));
        services.AddSingleton<IWhatsAppAutomationOrchestrator, WhatsAppAutomationOrchestrator>();
        services.AddSingleton<IWhatsAppCloudSender, MetaWhatsAppCloudSender>();
        return services;
    }
}

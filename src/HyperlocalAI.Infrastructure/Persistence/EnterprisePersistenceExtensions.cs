using HyperlocalAI.Application.Appointments;
using HyperlocalAI.Application.Dashboard;
using HyperlocalAI.Application.Leads;
using HyperlocalAI.Infrastructure.Appointments;
using HyperlocalAI.Infrastructure.Dashboard;
using HyperlocalAI.Infrastructure.Leads;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HyperlocalAI.Infrastructure.Persistence;

public static class EnterprisePersistenceExtensions
{
    /// <summary>SQLite (default) or SQL Server — set ConnectionStrings:LocalMind. Applies migrations at startup from the host.</summary>
    public static IServiceCollection AddLocalMindEnterprisePersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("LocalMind")
            ?? "Data Source=App_Data/localmind.db";

        services.AddDbContextFactory<LocalMindDbContext>(options => options.UseSqlite(cs));
        services.AddSingleton<IAppointmentBookingService, EfAppointmentBookingService>();
        services.AddSingleton<IBusinessMetricsRecorder, EnterpriseMetricsRecorder>();
        services.AddSingleton<IBusinessDashboardService, EfBusinessDashboardService>();
        services.AddSingleton<ILeadPipelineService, LeadPipelineService>();
        return services;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HyperlocalAI.Infrastructure.Persistence;

/// <summary>Design-time factory for <c>dotnet ef migrations</c>.</summary>
public sealed class LocalMindDbContextFactory : IDesignTimeDbContextFactory<LocalMindDbContext>
{
    public LocalMindDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LocalMindDbContext>()
            .UseSqlite("Data Source=App_Data/localmind.db")
            .Options;
        return new LocalMindDbContext(options);
    }
}

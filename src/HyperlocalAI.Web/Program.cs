using HyperlocalAI.Application;
using HyperlocalAI.Infrastructure;
using HyperlocalAI.Infrastructure.Persistence;
using HyperlocalAI.Web;
using HyperlocalAI.Web.Components;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HyperlocalAIOptions>(builder.Configuration.GetSection(HyperlocalAIOptions.SectionName));
builder.Services.AddLocalMindKnowledge(builder.Configuration);
builder.Services.AddLocalMindEnterprisePersistence(builder.Configuration);
builder.Services.AddLocalMindAutomation(builder.Configuration);
builder.Services.AddHttpClient("LocalMindOllama", client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
});
builder.Services.AddHttpClient("MetaWhatsApp", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddSingleton<IHyperlocalChatService, SemanticKernelChatService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

var appData = Path.Combine(app.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(appData);

await using (var scope = app.Services.CreateAsyncScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<LocalMindDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapGet("/chat", () => Results.Redirect("/"));

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapLocalMindAutomation();
app.MapLocalMindCrm();

app.Run();

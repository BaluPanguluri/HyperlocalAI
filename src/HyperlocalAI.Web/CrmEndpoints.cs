using HyperlocalAI.Application;
using HyperlocalAI.Application.Leads;

namespace HyperlocalAI.Web;

public static class CrmEndpoints
{
    public static void MapLocalMindCrm(this WebApplication app)
    {
        app.MapGet("/api/leads", async (
            BusinessVertical vertical,
            LeadStage? stage,
            int? page,
            int? pageSize,
            ILeadPipelineService leads,
            CancellationToken ct) =>
        {
            var p = page is > 0 ? page!.Value : 1;
            var ps = pageSize is > 0 ? pageSize!.Value : 25;
            var result = await leads.ListAsync(vertical, stage, p, ps, ct).ConfigureAwait(false);
            return Results.Json(result);
        });

        app.MapGet("/api/leads/{id:guid}", async (Guid id, ILeadPipelineService leads, CancellationToken ct) =>
        {
            var row = await leads.GetAsync(id, ct).ConfigureAwait(false);
            return row is null ? Results.NotFound() : Results.Json(row);
        });

        app.MapPatch("/api/leads/{id:guid}", async (Guid id, UpdateLeadRequest body, ILeadPipelineService leads, CancellationToken ct) =>
        {
            var row = await leads.UpdateAsync(id, body, ct).ConfigureAwait(false);
            return row is null ? Results.NotFound() : Results.Json(row);
        });
    }
}

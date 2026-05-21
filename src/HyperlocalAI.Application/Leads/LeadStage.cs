namespace HyperlocalAI.Application.Leads;

/// <summary>CRM pipeline stage for a captured lead (contact).</summary>
public enum LeadStage
{
    New = 0,
    Contacted = 1,
    Qualified = 2,
    Opportunity = 3,
    Won = 4,
    Lost = 5,
}

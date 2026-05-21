namespace HyperlocalAI.Infrastructure.Knowledge;

public sealed class KnowledgeStorageOptions
{
    public const string SectionName = "Knowledge";

    /// <summary>Folder under content root where each vertical has a subfolder (Shop, Clinic, RealEstate).</summary>
    public string RootRelativePath { get; set; } = "App_Data/tenant-documents";
}

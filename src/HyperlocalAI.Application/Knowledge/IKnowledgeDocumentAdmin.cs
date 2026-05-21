namespace HyperlocalAI.Application.Knowledge;

/// <summary>Upload/list/delete tenant documents (per business vertical).</summary>
public interface IKnowledgeDocumentAdmin
{
    Task<IReadOnlyList<KnowledgeFileInfo>> ListFilesAsync(BusinessVertical vertical, CancellationToken cancellationToken = default);

    Task UploadAsync(BusinessVertical vertical, string originalFileName, Stream content, CancellationToken cancellationToken = default);

    Task DeleteAsync(BusinessVertical vertical, string fileName, CancellationToken cancellationToken = default);
}

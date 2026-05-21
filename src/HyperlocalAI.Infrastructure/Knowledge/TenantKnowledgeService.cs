using HyperlocalAI.Application;
using HyperlocalAI.Application.Knowledge;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HyperlocalAI.Infrastructure.Knowledge;

public sealed class TenantKnowledgeService : ITenantKnowledgeProvider, IKnowledgeDocumentAdmin
{
    private static readonly string[] AllowedExtensions = [".txt", ".md", ".pdf"];

    private readonly IHostEnvironment _host;
    private readonly KnowledgeStorageOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TenantKnowledgeService> _logger;

    public TenantKnowledgeService(
        IOptions<KnowledgeStorageOptions> options,
        IHostEnvironment host,
        IMemoryCache cache,
        ILogger<TenantKnowledgeService> logger)
    {
        _options = options.Value;
        _host = host;
        _cache = cache;
        _logger = logger;
        EnsureRoot();
    }

    private string RootPath => Path.GetFullPath(Path.Combine(_host.ContentRootPath, _options.RootRelativePath));

    private static string VerticalFolder(BusinessVertical v) => v.ToString();

    private void EnsureRoot()
    {
        try
        {
            Directory.CreateDirectory(RootPath);
            foreach (BusinessVertical v in Enum.GetValues<BusinessVertical>())
                Directory.CreateDirectory(Path.Combine(RootPath, VerticalFolder(v)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create knowledge directories at {Root}", RootPath);
        }
    }

    public async Task<IReadOnlyList<KnowledgeChunk>> GetChunksForVerticalAsync(BusinessVertical vertical, CancellationToken cancellationToken = default)
    {
        var key = CacheKey(vertical);
        var result = await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(15);
            return await BuildChunksAsync(vertical, cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);

        return result ?? [];
    }

    private async Task<IReadOnlyList<KnowledgeChunk>> BuildChunksAsync(BusinessVertical vertical, CancellationToken cancellationToken)
    {
        var list = new List<KnowledgeChunk>();
        var dir = Path.Combine(RootPath, VerticalFolder(vertical));

        if (Directory.Exists(dir))
        {
            foreach (var path in Directory.EnumerateFiles(dir))
            {
                var ext = Path.GetExtension(path);
                if (!AllowedExtensions.Any(a => string.Equals(a, ext, StringComparison.OrdinalIgnoreCase)))
                    continue;

                try
                {
                    await using var fs = File.OpenRead(path);
                    if (fs.Length > KnowledgeUploadLimits.MaxBytes)
                    {
                        _logger.LogWarning("Skipping oversized knowledge file {File}", path);
                        continue;
                    }

                    var text = await DocumentTextExtractor.ExtractAsync(ext, fs, cancellationToken).ConfigureAwait(false);
                    var baseId = SanitizeId(Path.GetFileNameWithoutExtension(path));
                    var parts = KnowledgeTextChunker.ChunkText(text, baseId);
                    list.AddRange(parts);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load knowledge file {File}", path);
                }
            }
        }

        if (list.Count == 0)
            _logger.LogInformation("No knowledge documents for vertical {Vertical}. Upload .txt, .md, or .pdf on /knowledge.", vertical);

        return list;
    }

    private static string SanitizeId(string name)
    {
        var s = string.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrEmpty(s) ? "doc" : s[..Math.Min(s.Length, 80)];
    }

    private static string CacheKey(BusinessVertical v) => $"tenant-knowledge:{v}";

    private void Invalidate(BusinessVertical vertical) =>
        _cache.Remove(CacheKey(vertical));

    public Task<IReadOnlyList<KnowledgeFileInfo>> ListFilesAsync(BusinessVertical vertical, CancellationToken cancellationToken = default)
    {
        var dir = Path.Combine(RootPath, VerticalFolder(vertical));
        if (!Directory.Exists(dir))
            return Task.FromResult<IReadOnlyList<KnowledgeFileInfo>>(Array.Empty<KnowledgeFileInfo>());

        var list = new List<KnowledgeFileInfo>();
        foreach (var path in Directory.EnumerateFiles(dir))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ext = Path.GetExtension(path);
            if (!AllowedExtensions.Any(a => string.Equals(a, ext, StringComparison.OrdinalIgnoreCase)))
                continue;

            var fi = new FileInfo(path);
            list.Add(new KnowledgeFileInfo(fi.Name, fi.Length, fi.LastWriteTimeUtc));
        }

        return Task.FromResult<IReadOnlyList<KnowledgeFileInfo>>(list.OrderByDescending(f => f.LastModifiedUtc).ToList());
    }

    public async Task UploadAsync(BusinessVertical vertical, string originalFileName, Stream content, CancellationToken cancellationToken = default)
    {
        if (content.CanSeek && content.Length > KnowledgeUploadLimits.MaxBytes)
            throw new InvalidOperationException($"File exceeds maximum size ({KnowledgeUploadLimits.MaxBytes / 1024 / 1024} MB).");

        var safe = SafeFileName(originalFileName);
        var ext = Path.GetExtension(safe);
        if (AllowedExtensions.All(a => !string.Equals(a, ext, StringComparison.OrdinalIgnoreCase)))
            throw new NotSupportedException($"Allowed types: {string.Join(", ", AllowedExtensions)}");

        var dir = Path.Combine(RootPath, VerticalFolder(vertical));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, safe);

        await using (var fs = File.Create(path))
        {
            await content.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
        }

        Invalidate(vertical);
    }

    public Task DeleteAsync(BusinessVertical vertical, string fileName, CancellationToken cancellationToken = default)
    {
        var safe = SafeFileName(fileName);
        var path = Path.Combine(RootPath, VerticalFolder(vertical), safe);
        if (File.Exists(path))
            File.Delete(path);

        Invalidate(vertical);
        return Task.CompletedTask;
    }

    private static string SafeFileName(string name)
    {
        var f = Path.GetFileName(name);
        foreach (var c in Path.GetInvalidFileNameChars())
            f = f.Replace(c, '_');
        if (string.IsNullOrWhiteSpace(f))
            throw new ArgumentException("Invalid file name.");
        return f.Length > 160 ? f[..160] : f;
    }
}

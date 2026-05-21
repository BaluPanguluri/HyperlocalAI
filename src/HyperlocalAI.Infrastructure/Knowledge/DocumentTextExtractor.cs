using System.Text;
using UglyToad.PdfPig;

namespace HyperlocalAI.Infrastructure.Knowledge;

public static class DocumentTextExtractor
{
    public static async Task<string> ExtractAsync(string extension, Stream stream, CancellationToken cancellationToken)
    {
        extension = extension.ToLowerInvariant();
        return extension switch
        {
            ".txt" or ".md" => await ReadAllTextAsync(stream, cancellationToken).ConfigureAwait(false),
            ".pdf" => await Task.Run(() => ExtractPdf(stream), cancellationToken).ConfigureAwait(false),
            _ => throw new NotSupportedException($"Unsupported file type: {extension}"),
        };
    }

    private static async Task<string> ReadAllTextAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string ExtractPdf(Stream stream)
    {
        using var pdf = PdfDocument.Open(stream);
        var sb = new StringBuilder();
        foreach (var page in pdf.GetPages())
        {
            var t = page.Text;
            if (!string.IsNullOrWhiteSpace(t))
                sb.AppendLine(t);
        }

        return sb.ToString();
    }
}

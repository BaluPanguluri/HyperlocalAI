using System.Text;
using MiniExcelLibs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace HyperlocalAI.Web;

public static class ChatTranscriptExport
{
    public static byte[] ToUtf8Text(
        string verticalLabel,
        string businessName,
        IReadOnlyList<(string Role, string Text)> lines)
    {
        var sb = new StringBuilder();
        sb.AppendLine("LocalMind AI — chat export");
        sb.Append("Vertical: ").AppendLine(verticalLabel);
        sb.Append("Business: ").AppendLine(businessName);
        sb.Append("Exported (UTC): ").AppendLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        foreach (var (role, text) in lines)
        {
            sb.Append('[').Append(role).AppendLine("]");
            sb.AppendLine(text);
            sb.AppendLine();
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public static byte[] ToExcel(
        string verticalLabel,
        string businessName,
        IReadOnlyList<(string Role, string Text)> lines)
    {
        var rows = new List<object>
        {
            new { Role = "Vertical", Message = verticalLabel },
            new { Role = "Business", Message = businessName },
            new { Role = "Exported (UTC)", Message = DateTime.UtcNow.ToString("o") },
            new { Role = "—", Message = "—" },
        };
        foreach (var (role, text) in lines)
            rows.Add(new { Role = role, Message = text });

        using var ms = new MemoryStream();
        MiniExcel.SaveAs(ms, rows);
        return ms.ToArray();
    }

    public static byte[] ToPdf(
        string verticalLabel,
        string businessName,
        IReadOnlyList<(string Role, string Text)> lines)
    {
        using var ms = new MemoryStream();
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Text("LocalMind AI — chat export")
                    .SemiBold()
                    .FontSize(14);

                page.Content().Column(col =>
                {
                    col.Spacing(6);
                    col.Item().Text($"Vertical: {verticalLabel}");
                    col.Item().Text($"Business: {businessName}");
                    col.Item().Text($"Exported (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                    col.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);
                    foreach (var (role, text) in lines)
                    {
                        col.Item().PaddingTop(4).Text(t =>
                        {
                            t.Span($"{role}: ").SemiBold().FontColor(Colors.Blue.Medium);
                            t.Span(text);
                        });
                    }
                });
            });
        }).GeneratePdf(ms);

        return ms.ToArray();
    }
}

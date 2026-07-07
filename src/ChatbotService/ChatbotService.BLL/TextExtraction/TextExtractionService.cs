using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace ChatbotService.BLL.TextExtraction;

public sealed class TextExtractionService : ITextExtractionService
{
    public string Extract(string content, string? fileName, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return "";
        }

        var extension = Path.GetExtension(fileName ?? "").ToLowerInvariant();
        if (IsHtml(extension, contentType))
        {
            var withoutTags = Regex.Replace(content, "<[^>]+>", " ", RegexOptions.Compiled);
            return WebUtility.HtmlDecode(withoutTags);
        }

        if (extension == ".docx" || IsDocx(contentType))
        {
            return TryExtractBinary(content, ExtractDocx, "DOCX");
        }

        if (extension == ".pdf" || IsPdf(contentType))
        {
            return TryExtractBinary(content, ExtractPdf, "PDF");
        }

        return content;
    }

    private static bool IsHtml(string extension, string? contentType)
        => extension is ".html" or ".htm" || string.Equals(contentType, "text/html", StringComparison.OrdinalIgnoreCase);

    private static bool IsDocx(string? contentType)
        => string.Equals(contentType, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", StringComparison.OrdinalIgnoreCase);

    private static bool IsPdf(string? contentType)
        => string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase);

    private static string TryExtractBinary(string content, Func<byte[], string> extractor, string format)
    {
        try
        {
            return extractor(DecodeBinaryContent(content));
        }
        catch (Exception ex) when (ex is FormatException or InvalidDataException or IOException or ArgumentException)
        {
            throw new InvalidOperationException($"Unable to extract text from {format} content. Provide Base64 encoded binary content or pre-extracted text.", ex);
        }
    }

    private static byte[] DecodeBinaryContent(string content)
    {
        var trimmed = content.Trim();
        var commaIndex = trimmed.IndexOf(',', StringComparison.Ordinal);
        if (trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIndex >= 0)
        {
            trimmed = trimmed[(commaIndex + 1)..];
        }

        try
        {
            return Convert.FromBase64String(trimmed);
        }
        catch (FormatException ex)
        {
            throw new FormatException("Binary document content must be Base64 encoded.", ex);
        }
    }

    private static string ExtractDocx(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var document = WordprocessingDocument.Open(stream, false);
        return document.MainDocumentPart?.Document?.Body?.InnerText ?? "";
    }

    private static string ExtractPdf(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var document = PdfDocument.Open(stream);
        var builder = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            builder.AppendLine(page.Text);
        }

        return builder.ToString();
    }
}

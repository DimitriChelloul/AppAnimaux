using Shared.Semantic;

namespace ChatbotService.BLL.Services;

public sealed class DocumentChunker : ITextChunker
{
    public IReadOnlyList<string> Chunk(string text, int chunkSize, int chunkOverlap)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        chunkSize = Math.Max(100, chunkSize);
        chunkOverlap = Math.Clamp(chunkOverlap, 0, chunkSize / 2);

        var normalized = text.Replace("\r\n", "\n").Trim();
        var chunks = new List<string>();
        var start = 0;

        while (start < normalized.Length)
        {
            var length = Math.Min(chunkSize, normalized.Length - start);
            var end = start + length;

            if (end < normalized.Length)
            {
                var boundary = normalized.LastIndexOfAny(['.', '!', '?', '\n'], end - 1, length);
                if (boundary > start + chunkSize / 2)
                {
                    end = boundary + 1;
                }
            }

            chunks.Add(normalized[start..end].Trim());

            if (end >= normalized.Length)
            {
                break;
            }

            start = Math.Max(end - chunkOverlap, start + 1);
        }

        return chunks.Where(chunk => chunk.Length > 0).ToArray();
    }
}

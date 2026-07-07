using ChatbotService.BLL.Options;
using Microsoft.Extensions.Options;
using Shared.Semantic;

namespace ChatbotService.BLL.Services;

public sealed class ChunkingService
{
    private readonly ITextChunker _chunker;
    private readonly ChatbotRuntimeOptions _options;

    public ChunkingService(ITextChunker chunker, IOptions<ChatbotRuntimeOptions> options)
    {
        _chunker = chunker;
        _options = options.Value;
    }

    public IReadOnlyList<string> Chunk(string text) => _chunker.Chunk(text, _options.ChunkSize, _options.ChunkOverlap);
}

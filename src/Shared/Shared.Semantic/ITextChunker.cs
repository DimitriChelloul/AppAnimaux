namespace Shared.Semantic;

public interface ITextChunker
{
    IReadOnlyList<string> Chunk(string text, int chunkSize, int chunkOverlap);
}

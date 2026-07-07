namespace Shared.Semantic;

public sealed record EmbeddingVector
{
    public EmbeddingVector(IReadOnlyList<float> values)
    {
        Values = values;
    }

    public IReadOnlyList<float> Values { get; }
    public int Dimensions => Values.Count;

    public static EmbeddingVector Empty { get; } = new(Array.Empty<float>());
}

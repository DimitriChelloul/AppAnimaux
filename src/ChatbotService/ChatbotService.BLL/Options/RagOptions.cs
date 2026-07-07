namespace ChatbotService.BLL.Options;

public sealed class RagOptions
{
    public int TopK { get; set; } = 8;
    public int MaxContextChunks { get; set; } = 5;
    public double MinSimilarity { get; set; } = 0.68;
    public double VectorWeight { get; set; } = 0.78;
    public double TextWeight { get; set; } = 0.22;
    public int MaxContextTokens { get; set; } = 1800;
}

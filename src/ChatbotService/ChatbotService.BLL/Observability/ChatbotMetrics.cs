using System.Diagnostics.Metrics;

namespace ChatbotService.BLL.Observability;

public sealed class ChatbotMetrics
{
    public const string MeterName = "AppAnimaux.ChatbotService";

    private readonly Counter<long> _requests;
    private readonly Counter<long> _embeddings;
    private readonly Counter<long> _vectorSearches;
    private readonly Counter<long> _llmCalls;
    private readonly Histogram<double> _responseTimeMs;
    private readonly Histogram<long> _retrievedChunks;

    public ChatbotMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _requests = meter.CreateCounter<long>("chatbot.requests", description: "Number of chatbot ask requests.");
        _embeddings = meter.CreateCounter<long>("chatbot.embeddings", description: "Number of embeddings generated or served from cache.");
        _vectorSearches = meter.CreateCounter<long>("chatbot.vector_searches", description: "Number of vector or hybrid searches.");
        _llmCalls = meter.CreateCounter<long>("chatbot.llm_calls", description: "Number of LLM calls.");
        _responseTimeMs = meter.CreateHistogram<double>("chatbot.response_time_ms", unit: "ms", description: "Chatbot response time in milliseconds.");
        _retrievedChunks = meter.CreateHistogram<long>("chatbot.retrieved_chunks", description: "Number of chunks retrieved for a response.");
    }

    public void Request(bool emergency) => _requests.Add(1, new KeyValuePair<string, object?>("emergency", emergency));
    public void Embedding(string source) => _embeddings.Add(1, new KeyValuePair<string, object?>("source", source));
    public void VectorSearch(int chunkCount) { _vectorSearches.Add(1); _retrievedChunks.Record(chunkCount); }
    public void LlmCall() => _llmCalls.Add(1);
    public void ResponseTime(double elapsedMs) => _responseTimeMs.Record(elapsedMs);
}

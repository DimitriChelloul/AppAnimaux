using System.Net.Http.Headers;
using System.Net.Http.Json;
using ChatbotService.Api.Options;
using Microsoft.Extensions.Options;
using Shared.Semantic;

namespace ChatbotService.Api.Providers;

public sealed class OpenAiProvider : IEmbeddingProvider, ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;

    public OpenAiProvider(HttpClient httpClient, IOptions<OpenAiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<EmbeddingVector> GenerateEmbeddingAsync(string input, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        using var request = new HttpRequestMessage(HttpMethod.Post, "embeddings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent.Create(new
        {
            model = _options.EmbeddingModel,
            input
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: cancellationToken);
        var values = payload?.Data.FirstOrDefault()?.Embedding ?? Array.Empty<float>();

        return new EmbeddingVector(values);
    }

    public async Task<string> GenerateAnswerAsync(string prompt, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent.Create(new
        {
            model = _options.ChatModel,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.2
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: cancellationToken);
        return payload?.Choices.FirstOrDefault()?.Message.Content?.Trim() ?? "";
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("OpenAi:ApiKey is not configured.");
        }
    }

    private sealed record EmbeddingResponse(IReadOnlyList<EmbeddingData> Data);
    private sealed record EmbeddingData(float[] Embedding);
    private sealed record ChatCompletionResponse(IReadOnlyList<ChatChoice> Choices);
    private sealed record ChatChoice(ChatMessage Message);
    private sealed record ChatMessage(string Content);
}

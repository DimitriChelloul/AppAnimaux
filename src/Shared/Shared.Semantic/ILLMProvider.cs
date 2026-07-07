namespace Shared.Semantic;

public interface ILLMProvider
{
    Task<string> GenerateAnswerAsync(string prompt, CancellationToken cancellationToken = default);
}

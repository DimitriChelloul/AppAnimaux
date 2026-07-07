namespace ChatbotService.BLL.TextExtraction;

public interface ITextExtractionService
{
    string Extract(string content, string? fileName, string? contentType);
}

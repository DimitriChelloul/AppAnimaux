namespace ChatbotService.BLL.Security;

public sealed class InputSanitizer
{
    public string Sanitize(string input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "";
        }

        var normalized = input.Replace("\0", "").Replace("\r\n", "\n").Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }
}

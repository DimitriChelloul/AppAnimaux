namespace Shared.Config;

public static class OptionsValidation
{
    public static void RequireAbsoluteUri(Uri? uri, string settingName)
    {
        if (uri is null || !uri.IsAbsoluteUri)
        {
            throw new InvalidOperationException($"Configuration value '{settingName}' must be an absolute URI.");
        }
    }

    public static void RequireNotEmpty(string? value, string settingName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Configuration value '{settingName}' is required.");
        }
    }
}
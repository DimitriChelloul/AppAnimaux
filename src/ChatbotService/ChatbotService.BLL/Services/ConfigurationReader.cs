using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace ChatbotService.BLL.Services;

internal static class ConfigurationReader
{
    public static int GetInt(IConfiguration configuration, string key, int defaultValue)
        => int.TryParse(configuration[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : defaultValue;

    public static double GetDouble(IConfiguration configuration, string key, double defaultValue)
        => double.TryParse(configuration[key], NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : defaultValue;

    public static string GetString(IConfiguration configuration, string key, string defaultValue)
        => string.IsNullOrWhiteSpace(configuration[key]) ? defaultValue : configuration[key]!;
}

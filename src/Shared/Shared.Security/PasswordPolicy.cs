namespace Shared.Security;

public sealed class PasswordPolicy
{
    private static readonly string[] CommonPasswords =
    {
        "password", "password1", "password123", "azerty", "azerty123", "qwerty", "qwerty123",
        "123456", "123456789", "111111", "000000", "admin", "administrator", "letmein",
        "welcome", "iloveyou", "monkey", "dragon", "appanimaux"
    };

    public static PasswordPolicy Default { get; } = new();

    public int MinimumLength { get; init; } = 12;
    public int MaximumLength { get; init; } = 128;
    public bool RequireUppercase { get; init; } = true;
    public bool RequireLowercase { get; init; } = true;
    public bool RequireDigit { get; init; } = true;
    public bool RequireSymbol { get; init; } = true;

    public PasswordValidationResult Validate(string? password, string? email = null)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(password))
        {
            return PasswordValidationResult.Failure(new[] { "Password is required." });
        }

        if (password.Length < MinimumLength)
        {
            errors.Add($"Password must contain at least {MinimumLength} characters.");
        }

        if (password.Length > MaximumLength)
        {
            errors.Add($"Password must contain at most {MaximumLength} characters.");
        }

        if (password.Any(char.IsWhiteSpace))
        {
            errors.Add("Password must not contain whitespace.");
        }

        if (RequireUppercase && !password.Any(char.IsUpper))
        {
            errors.Add("Password must contain at least one uppercase letter.");
        }

        if (RequireLowercase && !password.Any(char.IsLower))
        {
            errors.Add("Password must contain at least one lowercase letter.");
        }

        if (RequireDigit && !password.Any(char.IsDigit))
        {
            errors.Add("Password must contain at least one digit.");
        }

        if (RequireSymbol && !password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            errors.Add("Password must contain at least one symbol.");
        }

        var normalized = password.ToLowerInvariant();
        if (CommonPasswords.Any(common => normalized.Contains(common, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add("Password is too common.");
        }

        var emailLocalPart = email?.Split('@', 2)[0];
        if (!string.IsNullOrWhiteSpace(emailLocalPart) && emailLocalPart.Length >= 3 && normalized.Contains(emailLocalPart.ToLowerInvariant()))
        {
            errors.Add("Password must not contain the email username.");
        }

        if (HasRepeatedRun(password, 4))
        {
            errors.Add("Password must not contain the same character repeated 4 times in a row.");
        }

        if (HasSequentialRun(password, 4))
        {
            errors.Add("Password must not contain obvious character sequences.");
        }

        return errors.Count == 0 ? PasswordValidationResult.Success : PasswordValidationResult.Failure(errors);
    }

    private static bool HasRepeatedRun(string value, int maxRun)
    {
        var run = 1;
        for (var i = 1; i < value.Length; i++)
        {
            run = char.ToLowerInvariant(value[i]) == char.ToLowerInvariant(value[i - 1]) ? run + 1 : 1;
            if (run >= maxRun) return true;
        }

        return false;
    }

    private static bool HasSequentialRun(string value, int runLength)
    {
        var normalized = value.ToLowerInvariant();
        for (var i = 0; i <= normalized.Length - runLength; i++)
        {
            var ascending = true;
            var descending = true;
            for (var j = 1; j < runLength; j++)
            {
                ascending &= normalized[i + j] == normalized[i + j - 1] + 1;
                descending &= normalized[i + j] == normalized[i + j - 1] - 1;
            }

            if (ascending || descending) return true;
        }

        return false;
    }
}
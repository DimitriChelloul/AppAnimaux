namespace Shared.Security;

public sealed record PasswordValidationResult(bool IsValid, IReadOnlyCollection<string> Errors)
{
    public static PasswordValidationResult Success { get; } = new(true, Array.Empty<string>());

    public static PasswordValidationResult Failure(IEnumerable<string> errors)
    {
        return new PasswordValidationResult(false, errors.ToArray());
    }
}
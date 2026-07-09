namespace Shared.Validation;

public sealed record ValidationIssue(string Field, string Message, string? Code = null);
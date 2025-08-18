namespace Business.Models;

/// <summary>
/// Represents the result of a validation operation with errors and warnings
/// </summary>
public class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public List<ValidationIssue> Errors { get; init; } = new();
    public List<ValidationIssue> Warnings { get; init; } = new();
}

/// <summary>
/// Represents a validation issue (error or warning)
/// </summary>
public class ValidationIssue
{
    public required string Message { get; init; }
    public string? DirectivePath { get; init; }
    public string? SourceFile { get; init; }
    public int? LineNumber { get; init; }
    public string? SourceContext { get; init; }
}
namespace ProjectDocumentationManager.Business.Models;

/// <summary>
/// Represents the result of a validation operation with errors and warnings
/// </summary>
public class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public List<ValidationIssue> Errors { get; init; } = new();
    public List<ValidationIssue> Warnings { get; init; } = new();
}

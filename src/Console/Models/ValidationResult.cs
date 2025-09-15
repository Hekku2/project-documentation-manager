namespace MarkdownCompiler.Console.Models;

/// <summary>
/// Represents the result of a validation operation with errors and warnings
/// </summary>
public class ValidationResult
{
    public bool IsValid => !Errors.Any();

    public int ValidFilesCount { get; set; }

    public int InvalidFilesCount => Errors.Select(e => e.SourceFile).Distinct().Count();

    public int WarningFilesCount => Warnings.Select(e => e.SourceFile).Distinct().Count();

    public List<ValidationIssue> Errors { get; init; } = new();
    public List<ValidationIssue> Warnings { get; init; } = new();
}
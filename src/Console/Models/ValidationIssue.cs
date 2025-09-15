namespace ProjectDocumentationManager.Console.Models;

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
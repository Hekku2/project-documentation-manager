namespace Business.Models;

/// <summary>
/// Represents a markdown document with its filename and content
/// </summary>
public class MarkdownDocument
{
    /// <summary>
    /// The filename of the document (e.g., "common-features.md")
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// The markdown content of the document
    /// </summary>
    public required string Content { get; init; }
}
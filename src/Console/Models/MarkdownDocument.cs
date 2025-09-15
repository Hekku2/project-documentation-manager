namespace MarkdownCompiler.Console.Models;

/// <summary>
/// Represents a markdown document with its filename, full path, and content
/// </summary>
public class MarkdownDocument
{
    /// <summary>
    /// The filename of the document (e.g., "common-features.md")
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// The relative file path of the document from the base directory (e.g., "docs/common-features.md")
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// The markdown content of the document
    /// </summary>
    public required string Content { get; init; }
}
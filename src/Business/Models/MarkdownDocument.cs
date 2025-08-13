namespace Business.Models;

/// <summary>
/// Represents a markdown document with its filename and content
/// </summary>
public class MarkdownDocument
{
    /// <summary>
    /// The filename of the document (e.g., "common-features.md")
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The markdown content of the document
    /// </summary>
    public string Content { get; set; } = string.Empty;

    public MarkdownDocument()
    {
    }

    public MarkdownDocument(string fileName, string content)
    {
        FileName = fileName;
        Content = content;
    }
}
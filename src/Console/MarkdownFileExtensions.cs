namespace ProjectDocumentationManager.Console;

/// <summary>
/// Constants for markdown file extensions used in the project documentation manager
/// </summary>
public static class MarkdownFileExtensions
{
    /// <summary>
    /// Template file extension for markdown template files
    /// </summary>
    public const string Template = ".mdext";

    /// <summary>
    /// Source file extension for markdown source files
    /// </summary>
    public const string Source = ".mdsrc";

    /// <summary>
    /// Standard markdown file extension
    /// </summary>
    public const string Markdown = ".md";

    /// <summary>
    /// Checks if the given file name has the specified extension
    /// </summary>
    /// <param name="name">The file name to check</param>
    /// <param name="ext">The file extension to check for</param>
    /// <param name="cmp">The string comparison options to use</param>
    /// <returns>True if the file name has the specified extension; otherwise, false.</returns>
    public static bool HasExtension(string name, string ext, StringComparison cmp = StringComparison.OrdinalIgnoreCase)
        => name?.EndsWith(ext, cmp) == true;
}
using ProjectDocumentationManager.Business.Models;

namespace ProjectDocumentationManager.Business.Services;

/// <summary>
/// Service for collecting markdown source and template files from a directory
/// </summary>
public interface IMarkdownFileCollectorService
{
    /// <summary>
    /// Collects all .mdext template files from the specified directory and its subdirectories
    /// </summary>
    /// <param name="directoryPath">Directory path to search for template files</param>
    /// <returns>Collection of MarkdownDocument objects representing template files</returns>
    /// <exception cref="ArgumentException">Thrown when directoryPath is null or empty</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when directory doesn't exist</exception>
    Task<IEnumerable<MarkdownDocument>> CollectTemplateFilesAsync(string directoryPath);

    /// <summary>
    /// Collects all .mdsrc source files from the specified directory and its subdirectories
    /// </summary>
    /// <param name="directoryPath">Directory path to search for source files</param>
    /// <returns>Collection of MarkdownDocument objects representing source files</returns>
    /// <exception cref="ArgumentException">Thrown when directoryPath is null or empty</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when directory doesn't exist</exception>
    Task<IEnumerable<MarkdownDocument>> CollectSourceFilesAsync(string directoryPath);

    /// <summary>
    /// Collects both template (.mdext) and source (.mdsrc) files from the specified directory
    /// </summary>
    /// <param name="directoryPath">Directory path to search for files</param>
    /// <returns>Tuple containing collections of template and source documents</returns>
    /// <exception cref="ArgumentException">Thrown when directoryPath is null or empty</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when directory doesn't exist</exception>
    Task<(IEnumerable<MarkdownDocument> TemplateFiles, IEnumerable<MarkdownDocument> SourceFiles)> CollectAllMarkdownFilesAsync(string directoryPath);
}
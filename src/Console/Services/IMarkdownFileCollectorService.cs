using MarkdownCompiler.Console.Models;

namespace MarkdownCompiler.Console.Services;

/// <summary>
/// Service for collecting markdown source and template files from a directory
/// </summary>
public interface IMarkdownFileCollectorService
{
    /// <summary>
    /// Collects all markdown files (.md, .mdsrc, and .mdext) from the specified directory
    /// </summary>
    /// <param name="directoryPath">Directory path to search for files</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of MarkdownDocument objects representing all markdown files</returns>
    /// <exception cref="ArgumentException">Thrown when directoryPath is null or empty</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when directory doesn't exist</exception>
    Task<IEnumerable<MarkdownDocument>> CollectAllMarkdownFilesAsync(string directoryPath, CancellationToken cancellationToken = default);
}
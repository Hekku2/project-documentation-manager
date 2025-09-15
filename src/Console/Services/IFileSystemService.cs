namespace MarkdownCompiler.Console.Services;

/// <summary>
/// Service for file system operations used by console commands
/// </summary>
public interface IFileSystemService
{
    /// <summary>
    /// Checks if a directory exists at the specified path
    /// </summary>
    /// <param name="path">The directory path to check</param>
    /// <returns>True if the directory exists, false otherwise</returns>
    bool DirectoryExists(string path);
    
    /// <summary>
    /// Ensures a directory exists at the specified path, creating it if necessary
    /// </summary>
    /// <param name="path">The directory path to ensure exists</param>
    void EnsureDirectoryExists(string path);
    
    /// <summary>
    /// Gets the absolute path for the specified path
    /// </summary>
    /// <param name="path">The path to get the absolute path for</param>
    /// <returns>The absolute path</returns>
    string GetFullPath(string path);
}
using System.Threading.Tasks;
using Desktop.Models;

namespace Desktop.Services;

public interface IFileService
{
    /// <summary>
    /// Gets the file structure for the configured project folder
    /// </summary>
    /// <returns>Tree-like structure of files and folders</returns>
    Task<FileSystemItem?> GetFileStructureAsync();

    /// <summary>
    /// Gets the file structure for a specific folder path
    /// </summary>
    /// <param name="folderPath">Path to the folder to scan</param>
    /// <returns>Tree-like structure of files and folders</returns>
    Task<FileSystemItem?> GetFileStructureAsync(string folderPath);

    /// <summary>
    /// Checks if a folder path exists and is accessible
    /// </summary>
    /// <param name="folderPath">Path to check</param>
    /// <returns>True if folder exists and is accessible</returns>
    bool IsValidFolder(string folderPath);

    /// <summary>
    /// Reads the content of a file asynchronously
    /// </summary>
    /// <param name="filePath">Path to the file to read</param>
    /// <returns>File content as string, or null if file cannot be read</returns>
    Task<string?> ReadFileContentAsync(string filePath);

    /// <summary>
    /// Writes content to a file asynchronously
    /// </summary>
    /// <param name="filePath">Path to the file to write</param>
    /// <param name="content">Content to write</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> WriteFileContentAsync(string filePath, string content);
}
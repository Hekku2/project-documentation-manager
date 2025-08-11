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
}
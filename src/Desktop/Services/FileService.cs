using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Desktop.Configuration;
using Desktop.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Desktop.Services;

public class FileService(ILogger<FileService> logger, IOptions<ApplicationOptions> options) : IFileService
{
    private readonly ApplicationOptions _options = options.Value;

    public async Task<FileSystemItem?> GetFileStructureAsync()
    {
        if (string.IsNullOrEmpty(_options.DefaultProjectFolder))
        {
            logger.LogWarning("DefaultProjectFolder is not configured in ApplicationOptions");
            return null;
        }

        return await GetFileStructureAsync(_options.DefaultProjectFolder);
    }

    public async Task<FileSystemItem?> GetFileStructureAsync(string folderPath)
    {
        if (!IsValidFolder(folderPath))
        {
            logger.LogWarning("Folder path is invalid or inaccessible: {FolderPath}", folderPath);
            return null;
        }

        try
        {
            logger.LogDebug("Building file structure for: {FolderPath}", folderPath);
            return await Task.Run(() => BuildFileStructure(folderPath));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error building file structure for: {FolderPath}", folderPath);
            return null;
        }
    }

    public bool IsValidFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return false;

        try
        {
            return Directory.Exists(folderPath);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error checking folder validity: {FolderPath}", folderPath);
            return false;
        }
    }

    private FileSystemItem BuildFileStructure(string path)
    {
        var directoryInfo = new DirectoryInfo(path);
        var item = new FileSystemItem
        {
            Name = directoryInfo.Name,
            FullPath = directoryInfo.FullName,
            IsDirectory = true,
            LastModified = directoryInfo.LastWriteTime,
            Size = 0
        };

        try
        {
            // Get subdirectories
            var directories = directoryInfo.GetDirectories()
                .Where(d => !IsHiddenOrSystem(d.Attributes) && (d.Attributes & FileAttributes.ReparsePoint) == 0)
                .OrderBy(d => d.Name);

            foreach (var directory in directories)
            {
                var childItem = BuildFileStructure(directory.FullName);
                item.Children.Add(childItem);
            }

            // Get files
            var files = directoryInfo.GetFiles()
                .Where(f => !IsHiddenOrSystem(f.Attributes) && (f.Attributes & FileAttributes.ReparsePoint) == 0)
                .OrderBy(f => f.Name);

            foreach (var file in files)
            {
                var fileItem = new FileSystemItem
                {
                    Name = file.Name,
                    FullPath = file.FullName,
                    IsDirectory = false,
                    LastModified = file.LastWriteTime,
                    Size = file.Length
                };
                item.Children.Add(fileItem);
            }

            logger.LogDebug("Built structure for {Path}: {DirectoryCount} directories, {FileCount} files",
                path, directories.Count(), files.Count());
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Access denied to directory: {Path}", path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing directory: {Path}", path);
        }

        return item;
    }

    public FileSystemItem? CreateFileSystemItem(string itemPath, bool isDirectory)
    {
        if (string.IsNullOrEmpty(itemPath))
            return null;

        if (isDirectory)
        {
            return BuildFileStructure(itemPath);
        }

        var fileName = Path.GetFileName(itemPath);
        if (string.IsNullOrEmpty(fileName))
            return null;

        var item = new FileSystemItem
        {
            Name = fileName,
            FullPath = itemPath,
            IsDirectory = isDirectory
        };

        try
        {
            item.LastModified = isDirectory ? Directory.GetLastWriteTime(itemPath) : File.GetLastWriteTime(itemPath);
            item.Size = isDirectory ? 0 : new FileInfo(itemPath).Length;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not get file system properties for {ItemPath}", itemPath);
            item.LastModified = DateTime.Now;
            item.Size = 0;
        }

        return item;
    }

    private static bool IsHiddenOrSystem(FileAttributes attributes)
    {
        return (attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0;
    }

    public async Task<string?> ReadFileContentAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            logger.LogWarning("File path is null or empty");
            return null;
        }

        try
        {
            if (!File.Exists(filePath))
            {
                logger.LogWarning("File does not exist: {FilePath}", filePath);
                return null;
            }

            logger.LogDebug("Reading file content: {FilePath}", filePath);
            return await File.ReadAllTextAsync(filePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading file: {FilePath}", filePath);
            return null;
        }
    }

    public async Task<bool> WriteFileContentAsync(string filePath, string content)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            logger.LogWarning("File path is null or empty");
            return false;
        }

        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            logger.LogDebug("Writing file content: {FilePath}", filePath);
            await File.WriteAllTextAsync(filePath, content ?? string.Empty);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error writing file: {FilePath}", filePath);
            return false;
        }
    }



    public async Task<bool> DeleteFolderContentsAsync(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            logger.LogWarning("Folder path is null or empty");
            return false;
        }

        // Resolve the absolute path and its root
        var fullPath = Path.GetFullPath(folderPath);
        var rootPath = Path.GetPathRoot(fullPath);

        // Refuse to operate on a drive or filesystem root
        if (string.Equals(
                Path.TrimEndingDirectorySeparator(fullPath),
                Path.TrimEndingDirectorySeparator(rootPath ?? string.Empty),
                StringComparison.OrdinalIgnoreCase))
        {
            logger.LogError(
                "Refusing to delete contents of a root directory: {FolderPath}",
                folderPath);
            return false;
        }

        // If the folder genuinely doesn't exist, treat it as "already clean"
        if (!Directory.Exists(fullPath))
        {
            logger.LogDebug(
                "Folder does not exist; treating as already clean: {FolderPath}",
                folderPath);
            return true;
        }

        try
        {
            logger.LogInformation("Deleting contents of folder: {FolderPath}", folderPath);

            await Task.Run(() =>
            {
                var directoryInfo = new DirectoryInfo(folderPath);

                // Delete all files
                foreach (var file in directoryInfo.GetFiles())
                {
                    try
                    {
                        file.Delete();
                        logger.LogDebug("Deleted file: {FilePath}", file.FullName);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to delete file: {FilePath}", file.FullName);
                    }
                }

                // Delete all subdirectories
                foreach (var directory in directoryInfo.GetDirectories())
                {
                    try
                    {
                        directory.Delete(true);
                        logger.LogDebug("Deleted directory: {DirectoryPath}", directory.FullName);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to delete directory: {DirectoryPath}", directory.FullName);
                    }
                }
            });

            logger.LogInformation("Successfully deleted contents of folder: {FolderPath}", folderPath);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting folder contents: {FolderPath}", folderPath);
            return false;
        }
    }

    public async Task<bool> CreateFileAsync(string folderPath, string fileName)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            logger.LogWarning("Folder path is null or empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            logger.LogWarning("File name is null or empty");
            return false;
        }

        if (!Directory.Exists(folderPath))
        {
            logger.LogWarning("Folder does not exist: {FolderPath}", folderPath);
            return false;
        }

        try
        {
            var filePath = Path.Combine(folderPath, fileName);

            if (File.Exists(filePath))
            {
                logger.LogWarning("File already exists: {FilePath}", filePath);
                return false;
            }

            logger.LogDebug("Creating new file: {FilePath}", filePath);
            await File.WriteAllTextAsync(filePath, string.Empty);
            logger.LogInformation("Successfully created file: {FilePath}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating file in folder: {FolderPath}", folderPath);
            return false;
        }
    }
}
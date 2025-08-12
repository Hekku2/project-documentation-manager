using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Desktop.Configuration;
using Desktop.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Desktop.Services;

public class FileService : IFileService
{
    private readonly ILogger<FileService> _logger;
    private readonly ApplicationOptions _options;

    public FileService(ILogger<FileService> logger, IOptions<ApplicationOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task<FileSystemItem?> GetFileStructureAsync()
    {
        if (string.IsNullOrEmpty(_options.DefaultProjectFolder))
        {
            _logger.LogWarning("DefaultProjectFolder is not configured in ApplicationOptions");
            return null;
        }

        return await GetFileStructureAsync(_options.DefaultProjectFolder);
    }

    public async Task<FileSystemItem?> GetFileStructureAsync(string folderPath)
    {
        if (!IsValidFolder(folderPath))
        {
            _logger.LogWarning("Folder path is invalid or inaccessible: {FolderPath}", folderPath);
            return null;
        }

        try
        {
            _logger.LogInformation("Building file structure for: {FolderPath}", folderPath);
            return await Task.Run(() => BuildFileStructure(folderPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building file structure for: {FolderPath}", folderPath);
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
            _logger.LogDebug(ex, "Error checking folder validity: {FolderPath}", folderPath);
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
                .Where(d => !IsHiddenOrSystem(d.Attributes))
                .OrderBy(d => d.Name);

            foreach (var directory in directories)
            {
                var childItem = BuildFileStructure(directory.FullName);
                item.Children.Add(childItem);
            }

            // Get files
            var files = directoryInfo.GetFiles()
                .Where(f => !IsHiddenOrSystem(f.Attributes))
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

            _logger.LogDebug("Built structure for {Path}: {DirectoryCount} directories, {FileCount} files", 
                path, directories.Count(), files.Count());
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied to directory: {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing directory: {Path}", path);
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
            _logger.LogWarning("File path is null or empty");
            return null;
        }

        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File does not exist: {FilePath}", filePath);
                return null;
            }

            _logger.LogDebug("Reading file content: {FilePath}", filePath);
            return await File.ReadAllTextAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file: {FilePath}", filePath);
            return null;
        }
    }

    public async Task<bool> WriteFileContentAsync(string filePath, string content)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogWarning("File path is null or empty");
            return false;
        }

        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _logger.LogDebug("Writing file content: {FilePath}", filePath);
            await File.WriteAllTextAsync(filePath, content ?? string.Empty);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing file: {FilePath}", filePath);
            return false;
        }
    }
}
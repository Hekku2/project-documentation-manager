using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Desktop.Services;

public class FileSystemMonitorService(ILogger<FileSystemMonitorService> logger) : IFileSystemMonitorService
{
    private FileSystemWatcher? _fileSystemWatcher;
    private bool _disposed = false;

    public event EventHandler<FileSystemChangedEventArgs>? FileSystemChanged;

    public bool IsMonitoring => _fileSystemWatcher?.EnableRaisingEvents == true;

    public void StartMonitoring(string folderPath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsMonitoring)
        {
            logger.LogDebug("File system monitoring is already active");
            return;
        }

        if (string.IsNullOrEmpty(folderPath) || !IsValidFolder(folderPath))
        {
            logger.LogWarning("Cannot start file system monitoring: invalid folder path");
            return;
        }

        try
        {
            var fullPath = Path.GetFullPath(folderPath);

            _fileSystemWatcher?.Dispose();
            _fileSystemWatcher = new FileSystemWatcher(fullPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
            };

            _fileSystemWatcher.Created += OnFileSystemChanged;
            _fileSystemWatcher.Deleted += OnFileSystemChanged;
            _fileSystemWatcher.Changed += OnFileSystemChanged;
            _fileSystemWatcher.Renamed += OnFileSystemRenamed;

            _fileSystemWatcher.EnableRaisingEvents = true;

            logger.LogInformation("Started file system monitoring for: {FolderPath} (resolved to: {FullPath})",
                folderPath, fullPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start file system monitoring");
            _fileSystemWatcher?.Dispose();
            _fileSystemWatcher = null;
        }
    }

    public void StopMonitoring()
    {
        if (_fileSystemWatcher != null)
        {
            _fileSystemWatcher.EnableRaisingEvents = false;
            _fileSystemWatcher.Dispose();
            _fileSystemWatcher = null;
            logger.LogInformation("Stopped file system monitoring");
        }
    }

    private bool IsValidFolder(string folderPath)
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

    private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
    {
        if (IsHiddenOrSystemPath(e.FullPath))
            return;

        var changeType = e.ChangeType switch
        {
            WatcherChangeTypes.Created => FileSystemChangeType.Created,
            WatcherChangeTypes.Deleted => FileSystemChangeType.Deleted,
            WatcherChangeTypes.Changed => FileSystemChangeType.Changed,
            _ => FileSystemChangeType.Changed
        };

        var isDirectory = Directory.Exists(e.FullPath) ||
                         (e.ChangeType == WatcherChangeTypes.Deleted && !Path.HasExtension(e.Name));

        var args = new FileSystemChangedEventArgs
        {
            ChangeType = changeType,
            Path = e.FullPath,
            IsDirectory = isDirectory
        };

        logger.LogDebug("File system change detected: {ChangeType} {Path} (IsDirectory: {IsDirectory})",
            changeType, e.FullPath, isDirectory);

        FileSystemChanged?.Invoke(this, args);
    }

    private void OnFileSystemRenamed(object sender, RenamedEventArgs e)
    {
        if (IsHiddenOrSystemPath(e.FullPath) || IsHiddenOrSystemPath(e.OldFullPath))
            return;

        var isDirectory = Directory.Exists(e.FullPath);

        var args = new FileSystemChangedEventArgs
        {
            ChangeType = FileSystemChangeType.Renamed,
            Path = e.FullPath,
            OldPath = e.OldFullPath,
            IsDirectory = isDirectory
        };

        logger.LogDebug("File system rename detected: {OldPath} -> {NewPath} (IsDirectory: {IsDirectory})",
            e.OldFullPath, e.FullPath, isDirectory);

        FileSystemChanged?.Invoke(this, args);
    }

    private static bool IsHiddenOrSystemPath(string path)
    {
        try
        {
            var fileName = Path.GetFileName(path);
            if (fileName.StartsWith('.') || fileName.StartsWith('~'))
                return true;

            if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                return IsHiddenOrSystem(fileInfo.Attributes);
            }
            else if (Directory.Exists(path))
            {
                var dirInfo = new DirectoryInfo(path);
                return IsHiddenOrSystem(dirInfo.Attributes);
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsHiddenOrSystem(FileAttributes attributes)
    {
        return (attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            StopMonitoring();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
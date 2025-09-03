using System;

namespace Desktop.Services;

public interface IFileSystemMonitorService : IDisposable
{
    event EventHandler<FileSystemChangedEventArgs>? FileSystemChanged;

    bool IsMonitoring { get; }

    void StartMonitoring(string folderPath);

    void StopMonitoring();
}
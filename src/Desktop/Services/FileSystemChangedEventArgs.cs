using System;

namespace Desktop.Services;

public class FileSystemChangedEventArgs : EventArgs
{
    public FileSystemChangeType ChangeType { get; init; }
    public string Path { get; init; } = string.Empty;
    public string? OldPath { get; init; }
    public bool IsDirectory { get; init; }
}

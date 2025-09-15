namespace MarkdownCompiler.Console.Services;

/// <summary>
/// Default implementation of IFileSystemService that wraps standard .NET file system operations
/// </summary>
public class FileSystemService : IFileSystemService
{
    public bool DirectoryExists(string path) => Directory.Exists(path);
    
    public void EnsureDirectoryExists(string path) => Directory.CreateDirectory(path);
    
    public string GetFullPath(string path) => Path.GetFullPath(path);
}
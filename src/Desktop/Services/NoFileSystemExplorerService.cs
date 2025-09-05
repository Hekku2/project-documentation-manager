namespace Desktop.Services;

public class NoFileSystemExplorerService : IFileSystemExplorerService
{
    public void ShowInExplorer(string filePath)
    {
        // No-op implementation for non-Windows platforms
    }
}

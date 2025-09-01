using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Desktop.Services;

public class WindowsFileSystemExplorerService(ILogger<WindowsFileSystemExplorerService> logger) : IFileSystemExplorerService
{
    // TODO: This should be only used in windows. If other OS is used, provide alternative implementation, or disable the functinality
    public void ShowInExplorer(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{filePath}\"") { UseShellExecute = true });
            }
            else if (Directory.Exists(filePath))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{filePath}\"") { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to open file explorer for path: {FilePath}", filePath);
        }
    }
}
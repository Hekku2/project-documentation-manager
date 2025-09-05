using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace Desktop.Services;

[ExcludeFromCodeCoverage]
[SupportedOSPlatform("windows")]
public class WindowsFileSystemExplorerService(ILogger<WindowsFileSystemExplorerService> logger) : IFileSystemExplorerService
{
    public void ShowInExplorer(string filePath)
    {
        try
        {
            if (!IsValidPath(filePath))
            {
                logger.LogError("Invalid file path provided to ShowInExplorer: {FilePath}", filePath);
                return;
            }

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

    private static bool IsValidPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;
        // Path must be absolute
        if (!Path.IsPathRooted(path))
            return false;
        // Check for invalid path chars
        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            return false;
        // Disallow quotes and other suspicious characters
        if (path.Contains("\"") || path.Contains(">") || path.Contains("<") || path.Contains("|"))
            return false;
        try
        {
            // This will throw if the path is invalid
            Path.GetFullPath(path);
        }
        catch
        {
            return false;
        }
        return true;
    }
}
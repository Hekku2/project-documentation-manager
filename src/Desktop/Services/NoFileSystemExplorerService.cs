namespace Desktop.Services;

using Microsoft.Extensions.Logging;
using System.Runtime.Versioning;

namespace Desktop.Services;

[UnsupportedOSPlatform("windows")]
public class NoFileSystemExplorerService(ILogger<NoFileSystemExplorerService> logger) : IFileSystemExplorerService
{
    public void ShowInExplorer(string filePath)
    {
        logger.LogInformation("ShowInExplorer not supported on this platform. Path: {Path}", filePath);
    }
}
}

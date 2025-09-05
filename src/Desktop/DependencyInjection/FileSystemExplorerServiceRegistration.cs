using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;

namespace Desktop.DependencyInjection;

public static class FileSystemExplorerServiceRegistration
{
    public static IServiceCollection AddFileSystemExplorerService(this IServiceCollection services)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            services.AddSingleton<Services.IFileSystemExplorerService, Services.WindowsFileSystemExplorerService>();
        else
            services.AddSingleton<Services.IFileSystemExplorerService, Services.NoFileSystemExplorerService>();
        return services;
    }
}

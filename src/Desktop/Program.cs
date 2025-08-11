using Avalonia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Desktop;

class Program
{
    [STAThread]
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
      
        // Set the service provider in the App
        App.ServiceProvider = host.Services;
        
        await host.StartAsync();
        
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            await host.StopAsync();
            host.Dispose();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                // Ensure appsettings.json is loaded from the application directory
                var appDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                config.SetBasePath(appDir!)
                      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // Register configuration options
                services.Configure<Desktop.Configuration.ApplicationOptions>(
                    context.Configuration.GetSection(nameof(Desktop.Configuration.ApplicationOptions)));
                
                // Register ViewModels
                services.AddScoped<Desktop.ViewModels.MainWindowViewModel>();
                
                // Register services here
                services.AddSingleton<Desktop.Views.MainWindow>();
                services.AddLogging(builder => builder.AddConsole());
                
                // Register application services
                services.AddScoped<Desktop.Services.IFileService, Desktop.Services.FileService>();
            });

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}

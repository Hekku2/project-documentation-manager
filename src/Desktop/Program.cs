using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Desktop.Logging;
using Desktop.DependencyInjection;

namespace Desktop;

class Program
{
    [STAThread]
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
      
        // Set the service provider in the App
        App.ServiceProvider = host.Services;
        
        // Initialize in-memory logging before starting the host
        var dynamicLoggerProvider = host.Services.GetRequiredService<IDynamicLoggerProvider>();
        var inMemoryLoggerProvider = host.Services.GetRequiredService<InMemoryLoggerProvider>();
        dynamicLoggerProvider.AddLoggerProvider(inMemoryLoggerProvider);
        
        await host.StartAsync();
        
        try
        {
            // Run Avalonia app with host cancellation token integration
            var app = BuildAvaloniaApp();
            var lifetime = new ClassicDesktopStyleApplicationLifetime()
            {
                Args = args,
                ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose
            };
            
            app.SetupWithLifetime(lifetime);
            
            // Connect host cancellation to Avalonia shutdown
            var hostLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            hostLifetime.ApplicationStopping.Register(() =>
            {
                if (lifetime.MainWindow != null)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        lifetime.MainWindow.Close();
                    });
                }
            });
            
            lifetime.Start(args);
        }
        finally
        {
            await host.StopAsync();
            host.Dispose();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices(ServiceConfiguration.ConfigureServices);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}

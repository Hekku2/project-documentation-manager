using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Desktop.Logging;

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
            .ConfigureServices((context, services) =>
            {
                // Register configuration options
                services.Configure<Desktop.Configuration.ApplicationOptions>(
                    context.Configuration.GetSection(nameof(Desktop.Configuration.ApplicationOptions)));
                
                // Register ViewModels
                services.AddSingleton<Desktop.ViewModels.MainWindowViewModel>();
                services.AddSingleton<Desktop.ViewModels.EditorTabBarViewModel>();
                services.AddSingleton<Desktop.ViewModels.EditorContentViewModel>();
                services.AddSingleton<Desktop.ViewModels.BuildConfirmationDialogViewModel>();
                
                // Register services here
                services.AddSingleton<Desktop.Views.MainWindow>();
                
                // Register logging components
                services.AddSingleton<InMemoryLoggerProvider>();
                services.AddSingleton<IDynamicLoggerProvider, DynamicLoggerProvider>();
                services.AddSingleton<ILogTransitionService, LogTransitionService>();
                services.AddLogging(builder => 
                {
                    builder.AddConsole();
                    builder.Services.AddSingleton<ILoggerProvider>(provider => 
                        provider.GetRequiredService<IDynamicLoggerProvider>());
                });
                
                // Register application services
                services.AddSingleton<Desktop.Services.IFileService, Desktop.Services.FileService>();
                services.AddSingleton<Desktop.Services.IEditorStateService, Desktop.Services.EditorStateService>();
                services.AddSingleton<Desktop.Services.IHotkeyService, Desktop.Services.HotkeyService>();
                
                // Register business services
                services.AddSingleton<Business.Services.IMarkdownCombinationService, Business.Services.MarkdownCombinationService>();
                services.AddSingleton<Business.Services.IMarkdownDocumentFileWriterService, Business.Services.MarkdownDocumentFileWriterService>();
                services.AddSingleton<Business.Services.IMarkdownFileCollectorService, Business.Services.MarkdownFileCollectorService>();
            });

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}

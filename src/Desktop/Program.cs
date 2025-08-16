using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Configuration;
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
                services.AddScoped<Desktop.ViewModels.BuildConfirmationDialogViewModel>();
                
                // Register services here
                services.AddSingleton<Desktop.Views.MainWindow>();
                
                // Register dynamic logging provider
                services.AddSingleton<IDynamicLoggerProvider, DynamicLoggerProvider>();
                services.AddLogging(builder => 
                {
                    builder.AddConsole();
                    builder.Services.AddSingleton<ILoggerProvider>(provider => 
                        provider.GetRequiredService<IDynamicLoggerProvider>());
                });
                
                // Register application services
                services.AddSingleton<Desktop.Services.IFileService, Desktop.Services.FileService>();
                
                // Register business services
                services.AddScoped<Business.Services.IMarkdownCombinationService, Business.Services.MarkdownCombinationService>();
                services.AddScoped<Business.Services.IMarkdownDocumentFileWriterService, Business.Services.MarkdownDocumentFileWriterService>();
                services.AddScoped<Business.Services.IMarkdownFileCollectorService, Business.Services.MarkdownFileCollectorService>();
            });

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Desktop.Logging;

namespace Desktop.DependencyInjection;

public static class ServiceConfiguration
{
    public static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        // Register configuration options
        services.Configure<Desktop.Configuration.ApplicationOptions>(
            context.Configuration.GetSection(nameof(Desktop.Configuration.ApplicationOptions)));
        
        // Register ViewModels
        services.AddSingleton<Desktop.ViewModels.MainWindowViewModel>();
        services.AddSingleton<Desktop.ViewModels.EditorTabBarViewModel>();
        services.AddSingleton<Desktop.ViewModels.EditorContentViewModel>();
        services.AddSingleton<Desktop.ViewModels.EditorViewModel>();
        services.AddSingleton<Desktop.ViewModels.FileExplorerViewModel>();
        services.AddTransient<Desktop.ViewModels.BuildConfirmationDialogViewModel>();
        
        // Register Views
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
        services.AddSingleton<Desktop.Services.IMarkdownRenderingService, Desktop.Services.MarkdownRenderingService>();
        services.AddSingleton<Desktop.Services.IFileSystemExplorerService, Desktop.Services.WindowsFileSystemExplorerService>();
        
        // Register factories
        services.AddSingleton<Desktop.Factories.FileSystemItemViewModelFactory>();
        
        // Register business services
        services.AddTransient<Business.Services.IMarkdownCombinationService, Business.Services.MarkdownCombinationService>();
        services.AddTransient<Business.Services.IMarkdownDocumentFileWriterService, Business.Services.MarkdownDocumentFileWriterService>();
        services.AddTransient<Business.Services.IMarkdownFileCollectorService, Business.Services.MarkdownFileCollectorService>();
    }
}
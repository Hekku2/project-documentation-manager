using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Desktop.Logging;
using Desktop.Factories;

namespace Desktop.DependencyInjection;

public static class ServiceConfiguration
{
    public static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        // Register configuration options
        services.Configure<Configuration.ApplicationOptions>(
            context.Configuration.GetSection(nameof(Configuration.ApplicationOptions)));
        
        // Register ViewModels
        services.AddSingleton<ViewModels.MainWindowViewModel>();
        services.AddSingleton<ViewModels.EditorTabBarViewModel>();
        services.AddSingleton<ViewModels.EditorContentViewModel>();
        services.AddSingleton<ViewModels.EditorViewModel>();
        services.AddSingleton<ViewModels.FileExplorerViewModel>();
        services.AddTransient<ViewModels.BuildConfirmationDialogViewModel>();
        
        // Register Views
        services.AddSingleton<Views.MainWindow>();
        
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
        services.AddSingleton<Services.IFileService, Services.FileService>();
        services.AddSingleton<Services.IEditorStateService, Services.EditorStateService>();
        services.AddSingleton<Services.IHotkeyService, Services.HotkeyService>();
        services.AddSingleton<Services.IMarkdownRenderingService, Services.MarkdownRenderingService>();
        services.AddSingleton<Services.IFileSystemExplorerService, Services.WindowsFileSystemExplorerService>();
        services.AddSingleton<Services.IFileSystemChangeHandler, Services.FileSystemChangeHandler>();
        
        // Register factories
        services.AddSingleton<ISettingsContentViewModelFactory, SettingsContentViewModelFactory>();
        
        // Note: IFileSystemItemViewModelFactory cannot be registered in DI because it requires 
        // runtime callback parameters (onItemSelected, onItemPreview)
        
        // Register business services
        services.AddTransient<Business.Services.IMarkdownCombinationService, Business.Services.MarkdownCombinationService>();
        services.AddTransient<Business.Services.IMarkdownDocumentFileWriterService, Business.Services.MarkdownDocumentFileWriterService>();
        services.AddTransient<Business.Services.IMarkdownFileCollectorService, Business.Services.MarkdownFileCollectorService>();
    }
}
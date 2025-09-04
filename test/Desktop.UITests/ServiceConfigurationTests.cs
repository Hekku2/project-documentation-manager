using Avalonia.Headless.NUnit;
using Desktop.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Desktop.UITests;

[TestFixture]
public class ServiceConfigurationTests
{
    [AvaloniaTest]
    public void DependencyInjection_Should_ResolveAllServices()
    {
        // Arrange - Build the host using the same configuration as the main program
        var hostBuilder = Host.CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices((context, services) =>
            {
                ServiceConfiguration.ConfigureServices(context, services);
            });
        using var host = hostBuilder.Build();
        var serviceProvider = host.Services;
        // Act & Assert - Verify all key services can be resolved
        Assert.Multiple(() =>
        {
            // ViewModels
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<Desktop.ViewModels.MainWindowViewModel>(),
                "MainWindowViewModel should be resolvable");
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<Desktop.ViewModels.EditorTabBarViewModel>(),
                "EditorTabBarViewModel should be resolvable");
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<Desktop.ViewModels.EditorContentViewModel>(),
                "EditorContentViewModel should be resolvable");
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<Desktop.ViewModels.BuildConfirmationDialogViewModel>(),
                "BuildConfirmationDialogViewModel should be resolvable");
            // Views
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<Desktop.Views.MainWindow>(),
                "MainWindow should be resolvable");
            // Application Services
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<Desktop.Services.IFileService>(),
                "IFileService should be resolvable");
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<Desktop.Services.IEditorStateService>(),
                "IEditorStateService should be resolvable");
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<Desktop.Services.IHotkeyService>(),
                "IHotkeyService should be resolvable");
            // Business Services
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<Business.Services.IMarkdownCombinationService>(),
                "IMarkdownCombinationService should be resolvable");
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<Business.Services.IMarkdownDocumentFileWriterService>(),
                "IMarkdownDocumentFileWriterService should be resolvable");
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<Business.Services.IMarkdownFileCollectorService>(),
                "IMarkdownFileCollectorService should be resolvable");
            // Logging Services
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<Desktop.Logging.IDynamicLoggerProvider>(),
                "IDynamicLoggerProvider should be resolvable");
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<Desktop.Logging.ILogTransitionService>(),
                "ILogTransitionService should be resolvable");
            // Configuration
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<IOptions<Desktop.Configuration.ApplicationOptions>>(),
                "ApplicationOptions should be resolvable");
        });
        // Verify multiple instances of transient services are different
        var service1 = serviceProvider.GetRequiredService<Desktop.ViewModels.BuildConfirmationDialogViewModel>();
        var service2 = serviceProvider.GetRequiredService<Desktop.ViewModels.BuildConfirmationDialogViewModel>();
        Assert.That(service1, Is.Not.SameAs(service2), "Transient services should return different instances");
        // Verify singleton services return the same instance
        var singleton1 = serviceProvider.GetRequiredService<Desktop.ViewModels.MainWindowViewModel>();
        var singleton2 = serviceProvider.GetRequiredService<Desktop.ViewModels.MainWindowViewModel>();
        Assert.That(singleton1, Is.SameAs(singleton2), "Singleton services should return the same instance");
    }
}

using System;
using Avalonia.Controls;
using Desktop.ViewModels;
using Desktop.Views;
using Desktop.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        
        // Subscribe to exit request
        viewModel.ExitRequested += OnExitRequested;
        viewModel.ShowBuildConfirmationDialog += OnShowBuildConfirmationDialog;
        
        // Initialize UI logging after component is loaded
        Loaded += OnWindowLoaded;
    }
    
    private void OnWindowLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Find the BottomOutput TextBox and set up UI logging
        var bottomOutput = this.FindControl<TextBox>("BottomOutput");
        if (bottomOutput != null && App.ServiceProvider != null)
        {
            var dynamicLoggerProvider = App.ServiceProvider.GetRequiredService<IDynamicLoggerProvider>();
            var uiLoggerProvider = new UILoggerProvider(bottomOutput);
            dynamicLoggerProvider.AddLoggerProvider(uiLoggerProvider);
            
            // Log a test message to show it's working
            var logger = App.ServiceProvider.GetRequiredService<ILogger<MainWindow>>();
            logger.LogInformation("UI logging initialized successfully!");
            logger.LogInformation("All application logs will now appear in the UI log output.");
        }
    }
    
    private void OnExitRequested(object? sender, EventArgs e)
    {
        Close();
    }

    private void OnShowBuildConfirmationDialog(object? sender, BuildConfirmationDialogViewModel dialogViewModel)
    {
        var dialog = new BuildConfirmationDialog(dialogViewModel);
        dialog.ShowDialog(this);
    }

}

using System;
using Avalonia.Controls;
using Desktop.ViewModels;
using Desktop.Views;
using Desktop.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Business.Models;

namespace Desktop.Views;

public partial class MainWindow : Window
{
    // Parameterless constructor for XAML designer support
    public MainWindow() : this(null!)
    {
    }

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
    
    private async void OnWindowLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
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
        
        // Initialize the view model after UI is fully loaded
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
    
    private void OnExitRequested(object? sender, EventArgs e)
    {
        Close();
    }

    private void OnShowBuildConfirmationDialog(object? sender, BuildConfirmationDialogViewModel dialogViewModel)
    {
        // Wire up validation results to the main window's error view
        dialogViewModel.ValidationResultsAvailable += OnValidationResultsAvailable;
        
        var dialog = new BuildConfirmationDialog(dialogViewModel);
        
        // Clean up the event subscription when dialog closes
        dialogViewModel.DialogClosed += (s, e) => {
            dialogViewModel.ValidationResultsAvailable -= OnValidationResultsAvailable;
        };
        
        dialog.ShowDialog(this);
    }
    
    private void OnValidationResultsAvailable(object? sender, ValidationResult validationResult)
    {
        // Pass validation results to the main window view model for display in error view
        if (DataContext is MainWindowViewModel mainViewModel)
        {
            mainViewModel.UpdateErrorPanelWithValidationResults(validationResult);
        }
    }

}
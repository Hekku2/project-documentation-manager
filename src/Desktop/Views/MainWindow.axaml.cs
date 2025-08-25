
using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Desktop.ViewModels;
using Desktop.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Business.Models;

namespace Desktop.Views;

public partial class MainWindow : Window
{
    private readonly FileExplorerViewModel? _fileExplorerViewModel;

    // Parameterless constructor for XAML designer support
    public MainWindow() : this(null!, null!)
    {
    }

    public MainWindow(MainWindowViewModel viewModel, FileExplorerViewModel fileExplorerViewModel)
    {
        _fileExplorerViewModel = fileExplorerViewModel;
        DataContext = viewModel;
        InitializeComponent();
        
        // Create and add the FileExplorer UserControl
        var fileExplorerControl = new FileExplorerUserControl(fileExplorerViewModel);
        var fileExplorerBorder = this.FindControl<Border>("FileExplorerBorder");
        if (fileExplorerBorder != null)
        {
            fileExplorerBorder.Child = fileExplorerControl;
        }
        
        // Wire up file selection from file explorer to main view model
        fileExplorerViewModel.FileSelected += async (sender, filePath) => await viewModel.EditorTabBar.OpenFileAsync(filePath);
        
        // Subscribe to exit request
        viewModel.ExitRequested += OnExitRequested;
        viewModel.ShowBuildConfirmationDialog += OnShowBuildConfirmationDialog;
        viewModel.HotkeysChanged += OnHotkeysChanged;
        
        // Initialize UI logging after component is loaded
        Loaded += OnWindowLoaded;
    }
    
    private async void OnWindowLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Find the BottomOutput TextBox within the BottomPanelUserControl and transition from in-memory to UI logging
        var bottomPanelUserControl = this.GetVisualDescendants().OfType<BottomPanelUserControl>().FirstOrDefault();
        var bottomOutput = bottomPanelUserControl?.FindControl<TextBox>("BottomOutput");
        if (bottomOutput != null && App.ServiceProvider != null)
        {
            var logTransitionService = App.ServiceProvider.GetRequiredService<ILogTransitionService>();
            logTransitionService.TransitionToUILogging(bottomOutput);
            
            // Log a test message to show it's working
            var logger = App.ServiceProvider.GetRequiredService<ILogger<MainWindow>>();
            logger.LogInformation("UI logging initialized successfully!");
            logger.LogInformation("Historical logs from application startup have been loaded.");
        }
        
        // Initialize the file explorer view model
        if (_fileExplorerViewModel != null)
        {
            await _fileExplorerViewModel.InitializeAsync();
        }
        
        // Apply hotkeys to this window
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ApplyHotkeysToWindow(this);
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

    private void OnHotkeysChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ApplyHotkeysToWindow(this);
        }
    }
}

using System;
using Avalonia.Controls;
using Desktop.ViewModels;
using Desktop.Views;

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
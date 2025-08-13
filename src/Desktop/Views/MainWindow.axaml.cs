
using System;
using Avalonia.Controls;
using Desktop.ViewModels;

namespace Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        
        // Subscribe to exit request
        viewModel.ExitRequested += OnExitRequested;
    }
    
    private void OnExitRequested(object? sender, EventArgs e)
    {
        Close();
    }
}
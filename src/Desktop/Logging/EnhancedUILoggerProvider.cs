using Microsoft.Extensions.Logging;
using Avalonia.Threading;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Desktop.Logging;

public class EnhancedUILoggerProvider(TextBox textBox) : ILoggerProvider
{
    private readonly TextBox _textBox = textBox ?? throw new ArgumentNullException(nameof(textBox));
    private readonly object _lock = new();
    private bool _disposed = false;

    public ILogger CreateLogger(string categoryName)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EnhancedUILoggerProvider));

        return new UILogger(categoryName, _textBox, _lock);
    }

    public void AddHistoricalLogs(IEnumerable<LogEntry> logEntries)
    {
        if (_disposed || !logEntries.Any())
            return;

        var formattedLogs = string.Join(Environment.NewLine, logEntries.Select(e => e.FormatLogEntry()));
        
        // Marshal to UI thread
        Dispatcher.UIThread.Post(() =>
        {
            lock (_lock)
            {
                try
                {
                    // Get the MainWindow and its ViewModel to access the log tab
                    var app = Avalonia.Application.Current;
                    var mainWindow = app?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
                        ? desktop.MainWindow as Desktop.Views.MainWindow 
                        : null;
                    
                    if (mainWindow?.DataContext is Desktop.ViewModels.MainWindowViewModel viewModel)
                    {
                        // Find the log tab and update its content (create if needed)
                        var logTab = viewModel.BottomPanelTabs.FirstOrDefault(t => t.Id == "logs");
                        if (logTab != null)
                        {
                            logTab.Content = formattedLogs + Environment.NewLine + logTab.Content;
                        }
                    }
                    else
                    {
                        // Fallback to direct TextBox update
                        _textBox.Text = formattedLogs + Environment.NewLine + _textBox.Text;
                    }
                    
                    // Auto-scroll to bottom if TextBox is visible
                    if (_textBox.Parent is ScrollViewer scrollViewer)
                    {
                        scrollViewer.ScrollToEnd();
                    }
                }
                catch
                {
                    // Ignore any UI-related exceptions to prevent logging loops
                }
            }
        });
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
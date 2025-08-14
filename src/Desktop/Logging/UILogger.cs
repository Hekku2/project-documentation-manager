using Microsoft.Extensions.Logging;
using Avalonia.Threading;
using Avalonia.Controls;
using System;
using System.Text;

namespace Desktop.Logging;

internal class UILogger : ILogger
{
    private readonly string _categoryName;
    private readonly TextBox _textBox;
    private readonly object _lock;

    public UILogger(string categoryName, TextBox textBox, object lockObject)
    {
        _categoryName = categoryName;
        _textBox = textBox;
        _lock = lockObject;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null; // Scopes not supported in this simple implementation
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= LogLevel.Debug; // Log everything for demo purposes
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var logEntry = FormatLogEntry(logLevel, _categoryName, message, exception);

        // Marshal to UI thread
        Dispatcher.UIThread.Post(() =>
        {
            lock (_lock)
            {
                try
                {
                    _textBox.Text += logEntry + Environment.NewLine;
                    
                    // Auto-scroll to bottom
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

    private static string FormatLogEntry(LogLevel logLevel, string categoryName, string message, Exception? exception)
    {
        var sb = new StringBuilder();
        
        // Timestamp
        sb.Append($"[{DateTime.Now:HH:mm:ss.fff}] ");
        
        // Log level with color indicators
        var levelText = logLevel switch
        {
            LogLevel.Critical => "CRIT",
            LogLevel.Error => "ERR ",
            LogLevel.Warning => "WARN",
            LogLevel.Information => "INFO",
            LogLevel.Debug => "DBG ",
            LogLevel.Trace => "TRC ",
            _ => "UNKN"
        };
        sb.Append($"[{levelText}] ");

        sb.Append($"{categoryName}: ");
        
        // Message
        sb.Append(message);
        
        // Exception if present
        if (exception != null)
        {
            sb.AppendLine();
            sb.Append($"    Exception: {exception.Message}");
        }
        
        return sb.ToString();
    }
}
using Microsoft.Extensions.Logging;
using Avalonia.Controls;
using System;

namespace Desktop.Logging;

public class UILoggerProvider(TextBox textBox) : ILoggerProvider
{
    private readonly TextBox _textBox = textBox ?? throw new ArgumentNullException(nameof(textBox));
    private readonly object _lock = new();
    private bool _disposed = false;

    public ILogger CreateLogger(string categoryName)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UILoggerProvider));

        return new UILogger(categoryName, _textBox, _lock);
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
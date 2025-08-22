using Microsoft.Extensions.Logging;
using Avalonia.Threading;
using Avalonia.Controls;
using System;

namespace Desktop.Logging;

public class UILoggerProvider : ILoggerProvider
{
    private readonly TextBox _textBox;
    private readonly object _lock = new();
    private bool _disposed = false;

    public UILoggerProvider(TextBox textBox)
    {
        _textBox = textBox ?? throw new ArgumentNullException(nameof(textBox));
    }

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
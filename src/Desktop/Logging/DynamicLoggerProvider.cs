using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Desktop.Logging;

public class DynamicLoggerProvider : IDynamicLoggerProvider
{
    private readonly ConcurrentBag<ILoggerProvider> _providers = new();
    private readonly ConcurrentDictionary<string, DynamicLogger> _loggers = new();
    private bool _disposed = false;

    public ILogger CreateLogger(string categoryName)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DynamicLoggerProvider));

        return _loggers.GetOrAdd(categoryName, name => new DynamicLogger(name, this));
    }

    public void AddLoggerProvider(ILoggerProvider provider)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DynamicLoggerProvider));

        _providers.Add(provider);
        
        // Update all existing loggers with the new provider
        foreach (var logger in _loggers.Values)
        {
            logger.AddProvider(provider);
        }
    }

    public void RemoveLoggerProvider(ILoggerProvider provider)
    {
        if (_disposed)
            return;

        // Note: ConcurrentBag doesn't support removal, so we'll mark as removed
        // In a production scenario, you might want to use a different collection
        foreach (var logger in _loggers.Values)
        {
            logger.RemoveProvider(provider);
        }
    }

    public void ClearProviders()
    {
        if (_disposed)
            return;

        foreach (var logger in _loggers.Values)
        {
            logger.ClearProviders();
        }
    }

    internal IEnumerable<ILoggerProvider> GetProviders()
    {
        return _providers.ToArray();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        
        foreach (var provider in _providers)
        {
            provider?.Dispose();
        }
        
        _loggers.Clear();
    }
}
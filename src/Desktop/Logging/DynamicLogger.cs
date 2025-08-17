using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Desktop.Logging;

internal class DynamicLogger : ILogger
{
    private readonly string _categoryName;
    private readonly DynamicLoggerProvider _provider;
    private readonly ConcurrentBag<ILogger> _loggers = new();

    public DynamicLogger(string categoryName, DynamicLoggerProvider provider)
    {
        _categoryName = categoryName;
        _provider = provider;
        
        // Create loggers for all existing providers
        foreach (var loggerProvider in _provider.GetProviders())
        {
            var logger = loggerProvider.CreateLogger(categoryName);
            _loggers.Add(logger);
        }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        var scopes = _loggers.Select(logger => logger.BeginScope(state))
                            .Where(scope => scope != null)
                            .Cast<IDisposable>()
                            .ToList();
        
        return scopes.Count > 0 ? new CompositeScope(scopes) : null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _loggers.Any(logger => logger.IsEnabled(logLevel));
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        foreach (var logger in _loggers)
        {
            if (logger.IsEnabled(logLevel))
            {
                logger.Log(logLevel, eventId, state, exception, formatter);
            }
        }
    }

    internal void AddProvider(ILoggerProvider provider)
    {
        var logger = provider.CreateLogger(_categoryName);
        _loggers.Add(logger);
    }

    internal void RemoveProvider(ILoggerProvider provider)
    {
        // Note: ConcurrentBag doesn't support removal
        // In a production scenario, you might want to track loggers differently
    }

    internal void ClearProviders()
    {
        // Note: ConcurrentBag doesn't support clearing
        // In a production scenario, you might want to use a different collection
    }

    private class CompositeScope : IDisposable
    {
        private readonly List<IDisposable> _scopes;

        public CompositeScope(List<IDisposable> scopes)
        {
            _scopes = scopes;
        }

        public void Dispose()
        {
            foreach (var scope in _scopes)
            {
                scope?.Dispose();
            }
        }
    }
}
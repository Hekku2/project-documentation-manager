using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Desktop.Logging;

public class InMemoryLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntry> _logEntries = new();
    private readonly ConcurrentDictionary<string, InMemoryLogger> _loggers = new();
    private bool _disposed = false;

    public ILogger CreateLogger(string categoryName)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(InMemoryLoggerProvider));

        return _loggers.GetOrAdd(categoryName, name => new InMemoryLogger(name, this));
    }

    internal void AddLogEntry(LogEntry logEntry)
    {
        if (_disposed)
            return;

        _logEntries.Enqueue(logEntry);
    }

    public IEnumerable<LogEntry> GetLogEntries()
    {
        return _logEntries.ToArray();
    }

    public string GetFormattedLogs()
    {
        var entries = GetLogEntries();
        return string.Join(Environment.NewLine, entries.Select(e => e.FormatLogEntry()));
    }

    public void ClearLogs()
    {
        while (_logEntries.TryDequeue(out _))
        {
            // Clear all entries
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _loggers.Clear();
    }
}
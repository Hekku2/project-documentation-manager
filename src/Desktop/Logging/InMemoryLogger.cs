using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace Desktop.Logging;

public class InMemoryLogger : ILogger
{
    private readonly string _categoryName;
    private readonly InMemoryLoggerProvider _provider;

    public InMemoryLogger(string categoryName, InMemoryLoggerProvider provider)
    {
        _categoryName = categoryName;
        _provider = provider;
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
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            LogLevel = logLevel,
            CategoryName = _categoryName,
            Message = message,
            Exception = exception
        };

        _provider.AddLogEntry(logEntry);
    }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel LogLevel { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }

    public string FormatLogEntry()
    {
        var sb = new StringBuilder();
        
        // Timestamp
        sb.Append($"[{Timestamp:HH:mm:ss.fff}] ");
        
        // Log level with color indicators
        var levelText = LogLevel switch
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

        sb.Append($"{CategoryName}: ");
        
        // Message
        sb.Append(Message);
        
        // Exception if present
        if (Exception != null)
        {
            sb.AppendLine();
            sb.Append($"    Exception: {Exception.Message}");
        }
        
        return sb.ToString();
    }
}
using Desktop.Logging;
using Microsoft.Extensions.Logging;

namespace Desktop.Controls;

public class LogDisplayEntry
{
    public LogDisplayEntry(LogEntry logEntry)
    {
        LogEntry = logEntry;
        TimestampText = $"[{logEntry.Timestamp:HH:mm:ss.fff}] ";
        LevelText = GetLevelText(logEntry.LogLevel);
        LevelColor = GetLevelColor(logEntry.LogLevel);
        CategoryText = $"{logEntry.CategoryName}: ";
        MessageText = logEntry.Message;
        ExceptionText = logEntry.Exception != null ? $"\n    Exception: {logEntry.Exception.Message}" : string.Empty;
    }

    public LogEntry LogEntry { get; }
    public string TimestampText { get; }
    public string LevelText { get; }
    public string LevelColor { get; }
    public string CategoryText { get; }
    public string MessageText { get; }
    public string ExceptionText { get; }

    private static string GetLevelText(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Critical => "[CRIT] ",
            LogLevel.Error => "[ERR ] ",
            LogLevel.Warning => "[WARN] ",
            LogLevel.Information => "[INFO] ",
            LogLevel.Debug => "[DBG ] ",
            LogLevel.Trace => "[TRC ] ",
            _ => "[UNKN] "
        };
    }

    private static string GetLevelColor(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Critical => "#FF0000", // Bright Red
            LogLevel.Error => "#FF6B6B",    // Red
            LogLevel.Warning => "#FFB347",  // Orange
            LogLevel.Information => "#4A9EFF", // Blue
            LogLevel.Debug => "#98FB98",    // Light Green
            LogLevel.Trace => "#DDA0DD",    // Plum
            _ => "#CCCCCC"                  // Default gray
        };
    }
}
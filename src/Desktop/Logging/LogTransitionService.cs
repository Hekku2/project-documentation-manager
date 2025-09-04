using System.Collections.Generic;
using System.Linq;

namespace Desktop.Logging;

public class LogTransitionService(IDynamicLoggerProvider dynamicLoggerProvider, InMemoryLoggerProvider inMemoryLoggerProvider) : ILogTransitionService
{
    private bool _hasTransitioned = false;

    public void TransitionToUILogging(Avalonia.Controls.TextBox textBox)
    {
        if (_hasTransitioned)
            return;

        // Create standard UI logger provider (not the enhanced one)
        var uiLoggerProvider = new UILoggerProvider(textBox);

        // Keep the in-memory provider active and add the UI provider
        // This ensures logs continue to be collected even when UI logger is active
        dynamicLoggerProvider.AddLoggerProvider(uiLoggerProvider);

        _hasTransitioned = true;
    }

    public IEnumerable<LogEntry> GetHistoricalLogs()
    {
        return inMemoryLoggerProvider.GetLogEntries();
    }

    public string GetFormattedHistoricalLogs()
    {
        var logs = GetHistoricalLogs();
        return string.Join(System.Environment.NewLine, logs.Select(e => e.FormatLogEntry()));
    }
}
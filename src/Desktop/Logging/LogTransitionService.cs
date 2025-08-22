using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Desktop.Logging;

public class LogTransitionService : ILogTransitionService
{
    private readonly IDynamicLoggerProvider _dynamicLoggerProvider;
    private readonly InMemoryLoggerProvider _inMemoryLoggerProvider;
    private bool _hasTransitioned = false;

    public LogTransitionService(IDynamicLoggerProvider dynamicLoggerProvider, InMemoryLoggerProvider inMemoryLoggerProvider)
    {
        _dynamicLoggerProvider = dynamicLoggerProvider;
        _inMemoryLoggerProvider = inMemoryLoggerProvider;
    }

    public void TransitionToUILogging(Avalonia.Controls.TextBox textBox)
    {
        if (_hasTransitioned)
            return;

        
        // Create standard UI logger provider (not the enhanced one)
        var uiLoggerProvider = new UILoggerProvider(textBox);
        
        // Keep the in-memory provider active and add the UI provider
        // This ensures logs continue to be collected even when UI logger is active
        _dynamicLoggerProvider.AddLoggerProvider(uiLoggerProvider);
        
        _hasTransitioned = true;
    }

    public IEnumerable<LogEntry> GetHistoricalLogs()
    {
        return _inMemoryLoggerProvider.GetLogEntries();
    }
    
    public string GetFormattedHistoricalLogs()
    {
        var logs = GetHistoricalLogs();
        return string.Join(System.Environment.NewLine, logs.Select(e => e.FormatLogEntry()));
    }
}
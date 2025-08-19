using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Desktop.Logging;

public class LogTransitionService : ILogTransitionService
{
    private readonly IDynamicLoggerProvider _dynamicLoggerProvider;
    private readonly InMemoryLoggerProvider _inMemoryLoggerProvider;
    private IEnumerable<LogEntry>? _historicalLogs;
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

        // Store historical logs before transitioning
        _historicalLogs = _inMemoryLoggerProvider.GetLogEntries().ToArray();
        
        // Create standard UI logger provider (not the enhanced one)
        var uiLoggerProvider = new UILoggerProvider(textBox);
        
        // Remove the in-memory provider and add the UI provider
        _dynamicLoggerProvider.RemoveLoggerProvider(_inMemoryLoggerProvider);
        _dynamicLoggerProvider.AddLoggerProvider(uiLoggerProvider);
        
        // Clear the in-memory logs to free memory
        _inMemoryLoggerProvider.ClearLogs();
        
        _hasTransitioned = true;
    }

    public IEnumerable<LogEntry> GetHistoricalLogs()
    {
        if (_hasTransitioned && _historicalLogs != null)
            return _historicalLogs;
        
        return _inMemoryLoggerProvider.GetLogEntries();
    }
    
    public string GetFormattedHistoricalLogs()
    {
        var logs = GetHistoricalLogs();
        return string.Join(System.Environment.NewLine, logs.Select(e => e.FormatLogEntry()));
    }
}
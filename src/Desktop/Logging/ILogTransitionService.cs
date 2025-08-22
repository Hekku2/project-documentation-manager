using System.Collections.Generic;

namespace Desktop.Logging;

public interface ILogTransitionService
{
    void TransitionToUILogging(Avalonia.Controls.TextBox textBox);
    IEnumerable<LogEntry> GetHistoricalLogs();
    string GetFormattedHistoricalLogs();
}
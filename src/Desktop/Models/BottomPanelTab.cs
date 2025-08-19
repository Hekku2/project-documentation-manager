using System.Collections.ObjectModel;
using Desktop.Logging;

namespace Desktop.Models;

public class BottomPanelTab
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public string? Content { get; set; }
    public bool IsActive { get; set; }
    public bool IsClosable { get; set; } = true;
    public ObservableCollection<ErrorEntry> ErrorEntries { get; set; } = new();
    public ObservableCollection<LogEntry> LogEntries { get; set; } = new();
}
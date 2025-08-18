using System.Collections.ObjectModel;

namespace Desktop.Models;

public class BottomPanelTab
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsClosable { get; set; } = true;
    public ObservableCollection<ErrorEntry> ErrorEntries { get; set; } = new();
}
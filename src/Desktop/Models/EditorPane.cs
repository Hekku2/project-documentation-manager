using System;
using System.Collections.ObjectModel;

namespace Desktop.Models;

public class EditorPane
{
    public required string Id { get; set; }
    public string? ActiveTabId { get; set; }
    public ObservableCollection<string> TabIds { get; set; } = [];
    public bool IsActive { get; set; }
    public PanePosition Position { get; set; } = new();
}

public class PanePosition
{
    public int Row { get; set; }
    public int Column { get; set; }
    public int RowSpan { get; set; } = 1;
    public int ColumnSpan { get; set; } = 1;
}
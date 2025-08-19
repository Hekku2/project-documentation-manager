namespace Desktop.Models;

public class EditorTab
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsModified { get; set; }
    public bool IsActive { get; set; }
    public TabType TabType { get; set; } = TabType.File;
}
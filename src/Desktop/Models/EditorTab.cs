namespace Desktop.Models;

public class EditorTab
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsModified { get; set; }
    public bool IsActive { get; set; }
    public required TabType TabType { get; set; }
}
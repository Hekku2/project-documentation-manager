namespace Desktop.Models;

public class EditorTab
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public string? FilePath { get; set; }
    public string? Content { get; set; }
    public bool IsModified { get; set; }
    public bool IsActive { get; set; }
    public required TabType TabType { get; set; }
}
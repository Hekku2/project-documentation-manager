using Desktop.Configuration;
using Desktop.ViewModels;
using Business.Models;

namespace Desktop.Models;

public abstract class EditorContentData
{
    public required EditorContentType ContentType { get; set; }
}

public class FileEditorContentData : EditorContentData
{
    public required EditorTabViewModel ActiveTab { get; set; }
    public ValidationResult? CurrentValidationResult { get; set; }
    public string? ActiveFilePath { get; set; }
}

public class SettingsEditorContentData : EditorContentData
{
    public required SettingsContentViewModel SettingsViewModel { get; set; }
    public ApplicationOptions ApplicationOptions => SettingsViewModel.ApplicationOptions;
}

public class FilePreviewContentData : EditorContentData
{
    public required string FilePath { get; set; }
    public required string FileContent { get; set; }
    public required string FileName { get; set; }
    public string? CompiledContent { get; set; }
    public bool IsCompiled { get; set; }
    public string? CompilationError { get; set; }
    
    /// <summary>
    /// Gets the content to display - compiled content if available and successful, otherwise raw content
    /// </summary>
    public string DisplayContent => IsCompiled && !string.IsNullOrEmpty(CompiledContent) ? CompiledContent : FileContent;
}
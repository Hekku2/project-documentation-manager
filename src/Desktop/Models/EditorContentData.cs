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
using System;
using Desktop.ViewModels;
using Business.Models;

namespace Desktop.Services;

public interface IEditorStateService
{
    EditorTabViewModel? ActiveTab { get; }
    string? ActiveFileContent { get; }
    string? ActiveFileName { get; }
    ValidationResult? CurrentValidationResult { get; set; }
    
    event EventHandler<EditorTabViewModel>? ActiveTabChanged;
    event EventHandler<ValidationResult>? ValidationResultChanged;
    
    void SetActiveTab(EditorTabViewModel? tab);
}
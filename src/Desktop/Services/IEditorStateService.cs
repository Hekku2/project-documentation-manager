using System;
using System.Collections.ObjectModel;
using Desktop.ViewModels;
using Business.Models;

namespace Desktop.Services;

public interface IEditorStateService
{
    EditorTabViewModel? ActiveTab { get; }
    string? ActiveFileContent { get; }
    string? ActiveFileName { get; }
    ValidationResult? CurrentValidationResult { get; set; }
    
    // Multi-pane support
    bool IsMultiPaneMode { get; }
    ObservableCollection<EditorPaneViewModel> Panes { get; }
    EditorPaneViewModel? ActivePane { get; }
    
    event EventHandler<EditorTabViewModel?>? ActiveTabChanged;
    event EventHandler<ValidationResult?>? ValidationResultChanged;
    event EventHandler<bool>? MultiPaneModeChanged;
    event EventHandler<EditorPaneViewModel?>? ActivePaneChanged;
    
    void SetActiveTab(EditorTabViewModel? tab);
    void SetActivePane(EditorPaneViewModel? pane);
    void EnableMultiPaneMode();
    void DisableMultiPaneMode();
    EditorPaneViewModel CreateNewPane();
    void RemovePane(EditorPaneViewModel pane);
}
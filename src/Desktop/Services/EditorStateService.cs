using System;
using Microsoft.Extensions.Logging;
using Desktop.ViewModels;
using Business.Models;

namespace Desktop.Services;

public class EditorStateService : IEditorStateService
{
    private readonly ILogger<EditorStateService> _logger;
    private EditorTabViewModel? _activeTab;
    private ValidationResult? _currentValidationResult;

    public EditorStateService(ILogger<EditorStateService> logger)
    {
        _logger = logger;
    }

    public EditorTabViewModel? ActiveTab
    {
        get => _activeTab;
        private set
        {
            if (_activeTab != value)
            {
                _activeTab = value;
                ActiveTabChanged?.Invoke(this, value);
            }
        }
    }

    public string? ActiveFileContent => ActiveTab?.Content;

    public string? ActiveFileName => ActiveTab != null ? System.IO.Path.GetFileName(ActiveTab.FilePath) : null;

    public ValidationResult? CurrentValidationResult
    {
        get => _currentValidationResult;
        set
        {
            if (_currentValidationResult != value)
            {
                _currentValidationResult = value;
                ValidationResultChanged?.Invoke(this, value);
            }
        }
    }

    public event EventHandler<EditorTabViewModel?>? ActiveTabChanged;
    public event EventHandler<ValidationResult?>? ValidationResultChanged;

    public void SetActiveTab(EditorTabViewModel? tab)
    {
        if (ActiveTab != null)
        {
            ActiveTab.IsActive = false;
        }

        if (tab != null)
        {
            tab.IsActive = true;
        }

        ActiveTab = tab;
        
        _logger.LogDebug("Active tab changed to: {TabTitle}", tab?.Title ?? "none");
    }
}
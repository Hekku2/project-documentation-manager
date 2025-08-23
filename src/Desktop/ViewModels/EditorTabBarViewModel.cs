using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Desktop.Models;
using Desktop.Services;

namespace Desktop.ViewModels;

public class EditorTabBarViewModel : ViewModelBase
{
    private readonly ILogger<EditorTabBarViewModel> _logger;
    private readonly IFileService _fileService;
    private readonly IEditorStateService _editorStateService;

    public EditorTabBarViewModel(
        ILogger<EditorTabBarViewModel> logger,
        IFileService fileService,
        IEditorStateService editorStateService)
    {
        _logger = logger;
        _fileService = fileService;
        _editorStateService = editorStateService;
        EditorTabs = new ObservableCollection<EditorTabViewModel>();
    }

    public ObservableCollection<EditorTabViewModel> EditorTabs { get; }

    public EditorTabViewModel? ActiveTab => _editorStateService.ActiveTab;

    public async Task OpenFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        // Normalize the file path for comparison
        var normalizedFilePath = Path.GetFullPath(filePath);

        // Check if tab already exists using normalized path comparison
        var existingTab = EditorTabs.FirstOrDefault(t => 
            string.Equals(Path.GetFullPath(t.FilePath), normalizedFilePath, StringComparison.OrdinalIgnoreCase));
        if (existingTab != null)
        {
            SetActiveTab(existingTab);
            return;
        }

        try
        {
            _logger.LogDebug("Opening file: {FilePath}", filePath);
            
            var content = await _fileService.ReadFileContentAsync(filePath);
            if (content == null)
            {
                _logger.LogWarning("Failed to read file content: {FilePath}", filePath);
                return;
            }

            var fileName = System.IO.Path.GetFileName(filePath);
            var tab = new EditorTab
            {
                Id = Guid.NewGuid().ToString(),
                Title = fileName,
                FilePath = normalizedFilePath,
                Content = content,
                IsModified = false,
                IsActive = true,
                TabType = TabType.File
            };

            var tabViewModel = new EditorTabViewModel(tab);
            tabViewModel.CloseRequested += OnTabCloseRequested;
            tabViewModel.SelectRequested += OnTabSelectRequested;
            EditorTabs.Add(tabViewModel);
            SetActiveTab(tabViewModel);
            
            _logger.LogDebug("File opened successfully: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening file: {FilePath}", filePath);
        }
    }

    public void SetActiveTab(EditorTabViewModel? tab)
    {
        _editorStateService.SetActiveTab(tab);
        OnPropertyChanged(nameof(ActiveTab));
    }

    private void OnTabCloseRequested(EditorTabViewModel tab)
    {
        CloseTab(tab);
    }

    private void OnTabSelectRequested(EditorTabViewModel tab)
    {
        SetActiveTab(tab);
    }

    public void CloseTab(EditorTabViewModel tab)
    {
        if (!EditorTabs.Contains(tab))
            return;

        // Unsubscribe from events to prevent memory leaks
        tab.CloseRequested -= OnTabCloseRequested;
        tab.SelectRequested -= OnTabSelectRequested;
        
        EditorTabs.Remove(tab);
        
        // If this was the active tab, set a new active tab
        if (ActiveTab == tab)
        {
            var newActiveTab = EditorTabs.FirstOrDefault();
            SetActiveTab(newActiveTab);
        }
        
        _logger.LogDebug("Tab closed: {TabTitle}", tab.Title);
    }

    private async Task<bool> SaveTabAsync(EditorTabViewModel tab)
    {
        if (tab == null || !tab.IsModified || tab.FilePath == null || tab.Content == null)
            return false;

        try
        {
            var success = await _fileService.WriteFileContentAsync(tab.FilePath, tab.Content);
            if (success)
            {
                tab.IsModified = false;
                _logger.LogDebug("File saved: {FilePath}", tab.FilePath);
            }
            else
            {
                _logger.LogWarning("Failed to save file: {FilePath}", tab.FilePath);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving file: {FilePath}", tab.FilePath);
            return false;
        }
    }

    public async Task SaveActiveFileAsync()
    {
        var activeTab = ActiveTab;
        if (activeTab == null)
            return;

        var success = await SaveTabAsync(activeTab);
        if (success)
        {
            _logger.LogInformation("File saved: {FilePath}", activeTab.FilePath);
        }
    }

    public async Task SaveAllAsync()
    {
        var modifiedTabs = EditorTabs.Where(tab => 
            tab.IsModified && 
            tab.FilePath != null && 
            tab.Content != null &&
            tab.TabType == TabType.File).ToList();

        if (!modifiedTabs.Any())
        {
            _logger.LogDebug("No modified files to save");
            return;
        }

        _logger.LogInformation("Saving {Count} modified files", modifiedTabs.Count);

        var saveResults = new List<(EditorTabViewModel tab, bool success)>();

        foreach (var tab in modifiedTabs)
        {
            var success = await SaveTabAsync(tab);
            saveResults.Add((tab, success));
        }

        var successCount = saveResults.Count(r => r.success);
        var failureCount = saveResults.Count(r => !r.success);

        if (failureCount == 0)
        {
            _logger.LogInformation("Successfully saved all {Count} files", successCount);
        }
        else
        {
            _logger.LogWarning("Save completed: {SuccessCount} successful, {FailureCount} failed", 
                successCount, failureCount);
        }
    }

    public void OpenSettingsTab()
    {
        // Check if settings tab already exists
        var existingTab = EditorTabs.FirstOrDefault(t => t.Id == "settings");
        if (existingTab != null)
        {
            SetActiveTab(existingTab);
            return;
        }

        try
        {
            _logger.LogDebug("Opening Settings tab");
            
            var tab = new EditorTab
            {
                Id = "settings",
                Title = "Settings",
                FilePath = null, // No file path for settings tab
                Content = null, // No content for settings tab
                IsModified = false,
                IsActive = true,
                TabType = TabType.Settings
            };

            var tabViewModel = new EditorTabViewModel(tab);
            tabViewModel.CloseRequested += OnTabCloseRequested;
            tabViewModel.SelectRequested += OnTabSelectRequested;
            EditorTabs.Add(tabViewModel);
            SetActiveTab(tabViewModel);
            
            _logger.LogDebug("Settings tab opened successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening Settings tab");
        }
    }
}
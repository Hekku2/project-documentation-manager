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

public class EditorTabBarViewModel(
    ILogger<EditorTabBarViewModel> logger,
    IFileService fileService,
    IEditorStateService editorStateService) : ViewModelBase
{
    public ObservableCollection<EditorTabViewModel> EditorTabs { get; } = [];

    public EditorTabViewModel? ActiveTab => editorStateService.ActiveTab;

    public async Task OpenFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        // Normalize the file path for comparison
        var normalizedFilePath = Path.GetFullPath(filePath);

        // Check if tab already exists using normalized path comparison
        var existingTab = EditorTabs.FirstOrDefault(t => 
            t.FilePath != null && string.Equals(Path.GetFullPath(t.FilePath), normalizedFilePath, StringComparison.OrdinalIgnoreCase));
        if (existingTab != null)
        {
            SetActiveTab(existingTab);
            return;
        }

        try
        {
            logger.LogDebug("Opening file: {FilePath}", filePath);
            
            var content = await fileService.ReadFileContentAsync(filePath);
            if (content == null)
            {
                logger.LogWarning("Failed to read file content: {FilePath}", filePath);
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
            tabViewModel.PreviewRequested += OnTabPreviewRequested;
            EditorTabs.Add(tabViewModel);
            SetActiveTab(tabViewModel);
            
            logger.LogDebug("File opened successfully: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error opening file: {FilePath}", filePath);
        }
    }

    public void SetActiveTab(EditorTabViewModel? tab)
    {
        editorStateService.SetActiveTab(tab);
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

    private async void OnTabPreviewRequested(EditorTabViewModel tab)
    {
        if (tab.FilePath != null)
        {
            await OpenFileInPreviewAsync(tab.FilePath);
        }
    }

    public void CloseTab(EditorTabViewModel tab)
    {
        if (!EditorTabs.Contains(tab))
            return;

        // Unsubscribe from events to prevent memory leaks
        tab.CloseRequested -= OnTabCloseRequested;
        tab.SelectRequested -= OnTabSelectRequested;
        tab.PreviewRequested -= OnTabPreviewRequested;
        
        EditorTabs.Remove(tab);
        
        // If this was the active tab, set a new active tab
        if (ActiveTab == tab)
        {
            var newActiveTab = EditorTabs.FirstOrDefault();
            SetActiveTab(newActiveTab);
        }
        
        logger.LogDebug("Tab closed: {TabTitle}", tab.Title);
    }

    private async Task<bool> SaveTabAsync(EditorTabViewModel tab)
    {
        if (tab == null || !tab.IsModified || tab.FilePath == null || tab.Content == null)
            return false;

        try
        {
            var success = await fileService.WriteFileContentAsync(tab.FilePath, tab.Content);
            if (success)
            {
                tab.IsModified = false;
                logger.LogDebug("File saved: {FilePath}", tab.FilePath);
            }
            else
            {
                logger.LogWarning("Failed to save file: {FilePath}", tab.FilePath);
            }
            return success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving file: {FilePath}", tab.FilePath);
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
            logger.LogInformation("File saved: {FilePath}", activeTab.FilePath);
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
            logger.LogDebug("No modified files to save");
            return;
        }

        logger.LogInformation("Saving {Count} modified files", modifiedTabs.Count);

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
            logger.LogInformation("Successfully saved all {Count} files", successCount);
        }
        else
        {
            logger.LogWarning("Save completed: {SuccessCount} successful, {FailureCount} failed", 
                successCount, failureCount);
        }
    }

    public async Task OpenFileInPreviewAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        // Normalize the file path for comparison
        var normalizedFilePath = Path.GetFullPath(filePath);
        var fileName = Path.GetFileName(normalizedFilePath);
        var previewTabId = $"preview_{normalizedFilePath.GetHashCode()}";

        // Check if preview tab already exists
        var existingTab = EditorTabs.FirstOrDefault(t => t.Id == previewTabId);
        if (existingTab != null)
        {
            SetActiveTab(existingTab);
            return;
        }

        try
        {
            logger.LogDebug("Opening file in preview: {FilePath}", filePath);
            
            var content = await fileService.ReadFileContentAsync(filePath);
            if (content == null)
            {
                logger.LogWarning("Failed to read file content for preview: {FilePath}", filePath);
                return;
            }

            var tab = new EditorTab
            {
                Id = previewTabId,
                Title = $"Preview: {fileName}",
                FilePath = normalizedFilePath,
                Content = content,
                IsModified = false,
                IsActive = true,
                TabType = TabType.Preview
            };

            var tabViewModel = new EditorTabViewModel(tab);
            tabViewModel.CloseRequested += OnTabCloseRequested;
            tabViewModel.SelectRequested += OnTabSelectRequested;
            tabViewModel.PreviewRequested += OnTabPreviewRequested;
            EditorTabs.Add(tabViewModel);
            SetActiveTab(tabViewModel);
            
            logger.LogDebug("File opened in preview successfully: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error opening file in preview: {FilePath}", filePath);
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
            logger.LogDebug("Opening Settings tab");
            
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
            tabViewModel.PreviewRequested += OnTabPreviewRequested;
            EditorTabs.Add(tabViewModel);
            SetActiveTab(tabViewModel);
            
            logger.LogDebug("Settings tab opened successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error opening Settings tab");
        }
    }
}
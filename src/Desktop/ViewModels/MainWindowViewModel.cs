using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Desktop.Configuration;
using Desktop.Services;
using System.Collections.ObjectModel;
using System.Linq;
using Desktop.Models;
using Business.Models;
using Desktop.Logging;
using Avalonia.Controls;

namespace Desktop.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly ApplicationOptions _applicationOptions;
    private readonly IEditorStateService _editorStateService;
    private readonly ILogTransitionService _logTransitionService;
    private readonly IHotkeyService _hotkeyService;
    private bool _isBottomPanelVisible = false;
    private BottomPanelTabViewModel? _activeBottomTab;
    private EditorTabViewModel? _currentlySubscribedTab;

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger, 
        IOptions<ApplicationOptions> applicationOptions, 
        IEditorStateService editorStateService,
        EditorTabBarViewModel editorTabBarViewModel,
        EditorContentViewModel editorContentViewModel,
        ILogTransitionService logTransitionService,
        IHotkeyService hotkeyService)
    {
        _logger = logger;
        _applicationOptions = applicationOptions.Value;
        _editorStateService = editorStateService;
        _logTransitionService = logTransitionService;
        _hotkeyService = hotkeyService;
        EditorTabBar = editorTabBarViewModel;
        EditorContent = editorContentViewModel;
        
        BottomPanelTabs = new ObservableCollection<BottomPanelTabViewModel>();
        ExitCommand = new RelayCommand(RequestApplicationExit);
        CloseBottomPanelCommand = new RelayCommand(CloseBottomPanel);
        ShowLogsCommand = new RelayCommand(ShowLogOutput);
        ShowErrorsCommand = new RelayCommand(ShowErrorOutput);
        SettingsCommand = new RelayCommand(OpenSettingsTab);
        SaveCommand = new RelayCommand(async () => await SaveActiveFileAsync(), CanSave);
        SaveAllCommand = new RelayCommand(async () => await SaveAllAsync(), CanSaveAll);
        ApplyHotkeyChangesCommand = new RelayCommand(ApplyHotkeyChanges);
        
        _logger.LogInformation("MainWindowViewModel initialized");
        _logger.LogInformation("Default project folder: {Folder}", _applicationOptions.DefaultProjectFolder);
        _logger.LogInformation("Default output folder: {OutputFolder}", _applicationOptions.DefaultOutputFolder);
        
        
        // Subscribe to validation result changes for error panel updates
        _editorStateService.ValidationResultChanged += OnValidationResultChanged;
        
        // Subscribe to active tab changes to update command states
        _editorStateService.ActiveTabChanged += OnActiveTabChangedForCommands;
        
        // Subscribe to tab collection changes to update SaveAll command state
        EditorTabBar.EditorTabs.CollectionChanged += OnEditorTabsCollectionChanged;
        
        // Subscribe to build confirmation dialog events
        EditorContent.ShowBuildConfirmationDialog += OnShowBuildConfirmationDialog;
        
        // Initialize bottom panel tabs
        InitializeBottomPanelTabs();
        
        // Initialize hotkeys
        InitializeHotkeys();
    }

    
    public ObservableCollection<BottomPanelTabViewModel> BottomPanelTabs { get; }
    
    public EditorTabBarViewModel EditorTabBar { get; }
    
    public EditorContentViewModel EditorContent { get; }

    public string Title => "Project Documentation Manager";
    
    public ApplicationOptions ApplicationOptions => _applicationOptions;




    public bool IsBottomPanelVisible
    {
        get => _isBottomPanelVisible;
        set => SetProperty(ref _isBottomPanelVisible, value);
    }

    public BottomPanelTabViewModel? ActiveBottomTab
    {
        get => _activeBottomTab;
        private set => SetProperty(ref _activeBottomTab, value);
    }

    
    public ICommand ExitCommand { get; }
    
    public ICommand CloseBottomPanelCommand { get; }
    
    public ICommand ShowLogsCommand { get; }
    
    public ICommand ShowErrorsCommand { get; }
    
    public ICommand SettingsCommand { get; }
    
    public ICommand SaveCommand { get; }
    
    public ICommand SaveAllCommand { get; }
    
    public ICommand ApplyHotkeyChangesCommand { get; }
    
    public event EventHandler? ExitRequested;
    public event EventHandler<BuildConfirmationDialogViewModel>? ShowBuildConfirmationDialog;
    public event EventHandler? HotkeysChanged;


    


    public async Task SaveActiveFileAsync()
    {
        await EditorTabBar.SaveActiveFileAsync();
    }

    public async Task SaveAllAsync()
    {
        await EditorTabBar.SaveAllAsync();
    }

    private bool CanSave()
    {
        var activeTab = _editorStateService.ActiveTab;
        return activeTab != null && 
               activeTab.FilePath != null && 
               activeTab.TabType == TabType.File &&
               activeTab.IsModified;
    }

    private bool CanSaveAll()
    {
        return EditorTabBar.EditorTabs.Any(tab => 
            tab.IsModified && 
            tab.FilePath != null && 
            tab.TabType == TabType.File);
    }

    private void OnActiveTabChangedForCommands(object? sender, EditorTabViewModel? activeTab)
    {
        // Unsubscribe from previous tab's property changes
        if (_currentlySubscribedTab != null)
        {
            _currentlySubscribedTab.PropertyChanged -= OnActiveTabPropertyChanged;
        }

        // Subscribe to new active tab's property changes
        _currentlySubscribedTab = activeTab;
        if (_currentlySubscribedTab != null)
        {
            _currentlySubscribedTab.PropertyChanged += OnActiveTabPropertyChanged;
        }

        // Update command states when active tab changes
        ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
        ((RelayCommand)SaveAllCommand).RaiseCanExecuteChanged();
    }

    private void OnActiveTabPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Update command states when IsModified property changes
        if (e.PropertyName == nameof(EditorTabViewModel.IsModified))
        {
            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            ((RelayCommand)SaveAllCommand).RaiseCanExecuteChanged();
        }
    }

    private void OnEditorTabsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Update SaveAll command state when tabs are added or removed
        ((RelayCommand)SaveAllCommand).RaiseCanExecuteChanged();

        // Subscribe to property changes for new tabs
        if (e.NewItems != null)
        {
            foreach (EditorTabViewModel tab in e.NewItems)
            {
                tab.PropertyChanged += OnAnyTabPropertyChanged;
            }
        }

        // Unsubscribe from property changes for removed tabs
        if (e.OldItems != null)
        {
            foreach (EditorTabViewModel tab in e.OldItems)
            {
                tab.PropertyChanged -= OnAnyTabPropertyChanged;
            }
        }
    }

    private void OnAnyTabPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Update SaveAll command state when any tab's IsModified property changes
        if (e.PropertyName == nameof(EditorTabViewModel.IsModified))
        {
            ((RelayCommand)SaveAllCommand).RaiseCanExecuteChanged();
        }
    }

    private void RequestApplicationExit()
    {
        _logger.LogInformation("Application exit requested");
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnShowBuildConfirmationDialog(object? sender, BuildConfirmationDialogViewModel dialogViewModel)
    {
        ShowBuildConfirmationDialog?.Invoke(this, dialogViewModel);
    }

    private void OnValidationResultChanged(object? sender, ValidationResult? validationResult)
    {
        if (validationResult != null)
        {
            UpdateErrorPanelWithValidationResults(validationResult);
        }
    }

    private void InitializeBottomPanelTabs()
    {
        // Tabs will be created on-demand when menu items are clicked
        // This allows tabs to be properly reopened after being closed
    }

    private void OnBottomTabCloseRequested(BottomPanelTabViewModel tab)
    {
        CloseBottomTab(tab);
    }

    private void OnBottomTabSelectRequested(BottomPanelTabViewModel tab)
    {
        SetActiveBottomTab(tab);
    }

    public void SetActiveBottomTab(BottomPanelTabViewModel tab)
    {
        // Deactivate current active tab
        if (ActiveBottomTab != null)
        {
            ActiveBottomTab.IsActive = false;
        }

        // Set new active tab
        tab.IsActive = true;
        ActiveBottomTab = tab;
        IsBottomPanelVisible = true;
        
        _logger.LogDebug("Active bottom tab changed to: {TabTitle}", tab.Title);
    }

    public void CloseBottomTab(BottomPanelTabViewModel tab)
    {
        if (!BottomPanelTabs.Contains(tab))
            return;

        // Unsubscribe from events to prevent memory leaks
        tab.CloseRequested -= OnBottomTabCloseRequested;
        tab.SelectRequested -= OnBottomTabSelectRequested;
        
        BottomPanelTabs.Remove(tab);
        
        // If this was the active tab, set a new active tab or hide the panel
        if (ActiveBottomTab == tab)
        {
            ActiveBottomTab = BottomPanelTabs.FirstOrDefault();
            if (ActiveBottomTab != null)
            {
                ActiveBottomTab.IsActive = true;
                IsBottomPanelVisible = true;
            }
            else
            {
                IsBottomPanelVisible = false;
            }
        }
        
        _logger.LogDebug("Bottom tab closed: {TabTitle}", tab.Title);
    }

    private void CloseBottomPanel()
    {
        if (ActiveBottomTab != null)
        {
            ActiveBottomTab.IsActive = false;
            ActiveBottomTab = null;
        }
        IsBottomPanelVisible = false;
        _logger.LogInformation("Bottom panel closed");
    }
    
    private BottomPanelTabViewModel GetOrCreateBottomTab(string id, string title)
    {
        var existingTab = BottomPanelTabs.FirstOrDefault(t => t.Id == id);
        if (existingTab != null)
            return existingTab;

        // Create new tab if it doesn't exist
        var tabModel = new BottomPanelTab
        {
            Id = id,
            Title = title,
            Content = GetInitialContentForTab(id),
            IsActive = false,
            IsClosable = true
        };
        
        // Populate LogEntries for log tab
        if (id == "logs")
        {
            var historicalLogs = _logTransitionService.GetHistoricalLogs();
            foreach (var logEntry in historicalLogs)
            {
                tabModel.LogEntries.Add(logEntry);
            }
        }
        
        var tabViewModel = new BottomPanelTabViewModel(tabModel);
        tabViewModel.CloseRequested += OnBottomTabCloseRequested;
        tabViewModel.SelectRequested += OnBottomTabSelectRequested;
        BottomPanelTabs.Add(tabViewModel);
        
        return tabViewModel;
    }
    
    private string GetInitialContentForTab(string id)
    {
        if (id == "logs")
        {
            // Get historical logs for the log tab with trailing newline
            var historicalLogs = _logTransitionService.GetFormattedHistoricalLogs();
            return string.IsNullOrEmpty(historicalLogs) ? "" : historicalLogs + Environment.NewLine;
        }
        
        return "";
    }

    private void ShowLogOutput()
    {
        var logTab = GetOrCreateBottomTab("logs", "Log Output");
        SetActiveBottomTab(logTab);
        _logger.LogInformation("Log output tab shown");
    }
    
    private void ShowErrorOutput()
    {
        var errorTab = GetOrCreateBottomTab("errors", "Errors");
        SetActiveBottomTab(errorTab);
        _logger.LogInformation("Error output tab shown");
    }

    public void UpdateErrorPanelWithValidationResults(ValidationResult validationResult)
    {
        // Only show the error panel if there are actual errors or warnings
        if (!validationResult.IsValid)
        {
            var errorTab = GetOrCreateBottomTab("errors", "Errors");
            var errorContent = new System.Text.StringBuilder();
            
            // Clear existing error entries
            errorTab.ErrorEntries.Clear();
            
            // Add errors
            foreach (var error in validationResult.Errors)
            {
                var lineInfo = error.LineNumber.HasValue ? $" (Line {error.LineNumber})" : "";
                errorContent.AppendLine($"Error: {error.Message}{lineInfo}");
                if (!string.IsNullOrEmpty(error.DirectivePath))
                {
                    errorContent.AppendLine($"  File: {error.DirectivePath}");
                }
                if (!string.IsNullOrEmpty(error.SourceContext))
                {
                    errorContent.AppendLine($"  Context: {error.SourceContext}");
                }
                errorContent.AppendLine();
                
                // Create ErrorEntry for navigation
                // Prefer SourceFile (where error occurs) over DirectivePath (what file is referenced)
                var navigationPath = error.SourceFile ?? error.DirectivePath;
                var errorEntry = new ErrorEntry
                {
                    Type = "Error",
                    Message = error.Message,
                    FilePath = navigationPath,
                    FileName = !string.IsNullOrEmpty(navigationPath) ? System.IO.Path.GetFileName(navigationPath) : null,
                    LineNumber = error.LineNumber,
                    SourceContext = error.SourceContext,
                    NavigateCommand = !string.IsNullOrEmpty(navigationPath) 
                        ? new RelayCommand(() => NavigateToFile(navigationPath!))
                        : null
                };
                errorTab.ErrorEntries.Add(errorEntry);
            }
            
            // Add warnings
            foreach (var warning in validationResult.Warnings)
            {
                var lineInfo = warning.LineNumber.HasValue ? $" (Line {warning.LineNumber})" : "";
                errorContent.AppendLine($"Warning: {warning.Message}{lineInfo}");
                if (!string.IsNullOrEmpty(warning.DirectivePath))
                {
                    errorContent.AppendLine($"  File: {warning.DirectivePath}");
                }
                if (!string.IsNullOrEmpty(warning.SourceContext))
                {
                    errorContent.AppendLine($"  Context: {warning.SourceContext}");
                }
                errorContent.AppendLine();
                
                // Create ErrorEntry for navigation
                // Prefer SourceFile (where warning occurs) over DirectivePath (what file is referenced)
                var navigationPath = warning.SourceFile ?? warning.DirectivePath;
                var warningEntry = new ErrorEntry
                {
                    Type = "Warning",
                    Message = warning.Message,
                    FilePath = navigationPath,
                    FileName = !string.IsNullOrEmpty(navigationPath) ? System.IO.Path.GetFileName(navigationPath) : null,
                    LineNumber = warning.LineNumber,
                    SourceContext = warning.SourceContext,
                    NavigateCommand = !string.IsNullOrEmpty(navigationPath) 
                        ? new RelayCommand(() => NavigateToFile(navigationPath!))
                        : null
                };
                errorTab.ErrorEntries.Add(warningEntry);
            }
            
            errorTab.Content = errorContent.ToString().TrimEnd();
            
            // Show the error panel only when there are errors or warnings
            SetActiveBottomTab(errorTab);
        }
        // If validation passes, don't create or show the error panel
    }

    private async void NavigateToFile(string filePath)
    {
        try
        {
            _logger.LogInformation("Navigating to file from error: {FilePath}", filePath);
            await EditorTabBar.OpenFileAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to file: {FilePath}", filePath);
        }
    }

    private void OpenSettingsTab()
    {
        _logger.LogInformation("Opening Settings tab");
        EditorTabBar.OpenSettingsTab();
    }

    private void InitializeHotkeys()
    {
        var commands = new Dictionary<string, ICommand>
        {
            ["Save"] = SaveCommand,
            ["SaveAll"] = SaveAllCommand,
            ["BuildDocumentation"] = EditorContent.BuildDocumentationCommand
        };

        _hotkeyService.RegisterHotkeys(_applicationOptions.Hotkeys, commands);
        _logger.LogInformation("Hotkeys initialized from settings");
    }

    public void ApplyHotkeysToWindow(Window window)
    {
        _hotkeyService.ApplyToWindow(window);
        _logger.LogDebug("Hotkeys applied to window");
    }

    private void ApplyHotkeyChanges()
    {
        try
        {
            // Re-register all hotkeys with the current settings
            InitializeHotkeys();
            
            // Notify that hotkeys have changed so the window can reapply them
            HotkeysChanged?.Invoke(this, EventArgs.Empty);
            
            _logger.LogInformation("Hotkey changes applied successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying hotkey changes");
        }
    }
}
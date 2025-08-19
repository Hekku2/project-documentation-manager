using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Desktop.Configuration;
using Desktop.Services;
using System.Collections.ObjectModel;
using System.Linq;
using Desktop.Models;
using Microsoft.Extensions.DependencyInjection;
using Business.Services;
using Business.Models;
using Desktop.Logging;

namespace Desktop.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly ApplicationOptions _applicationOptions;
    private readonly IFileService _fileService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEditorStateService _editorStateService;
    private readonly ILogTransitionService _logTransitionService;
    private bool _isLoading;
    private FileSystemItemViewModel? _rootItem;
    private bool _isBottomPanelVisible = false;
    private BottomPanelTabViewModel? _activeBottomTab;

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger, 
        IOptions<ApplicationOptions> applicationOptions, 
        IFileService fileService,
        IServiceProvider serviceProvider,
        IEditorStateService editorStateService,
        EditorTabBarViewModel editorTabBarViewModel,
        EditorContentViewModel editorContentViewModel,
        ILogTransitionService logTransitionService)
    {
        _logger = logger;
        _applicationOptions = applicationOptions.Value;
        _fileService = fileService;
        _serviceProvider = serviceProvider;
        _editorStateService = editorStateService;
        _logTransitionService = logTransitionService;
        EditorTabBar = editorTabBarViewModel;
        EditorContent = editorContentViewModel;
        
        FileSystemItems = new ObservableCollection<FileSystemItemViewModel>();
        BottomPanelTabs = new ObservableCollection<BottomPanelTabViewModel>();
        ExitCommand = new RelayCommand(RequestApplicationExit);
        CloseBottomPanelCommand = new RelayCommand(CloseBottomPanel);
        ShowLogsCommand = new RelayCommand(ShowLogOutput);
        ShowErrorsCommand = new RelayCommand(ShowErrorOutput);
        
        _logger.LogInformation("MainWindowViewModel initialized");
        _logger.LogInformation("Default theme: {Theme}", _applicationOptions.DefaultTheme);
        _logger.LogInformation("Default project folder: {Folder}", _applicationOptions.DefaultProjectFolder);
        _logger.LogInformation("Default output folder: {OutputFolder}", _applicationOptions.DefaultOutputFolder);
        
        // Subscribe to file selection events
        FileSystemItemViewModel.FileSelected += OnFileSelected;
        
        // Subscribe to validation result changes for error panel updates
        _editorStateService.ValidationResultChanged += OnValidationResultChanged;
        
        // Subscribe to build confirmation dialog events
        EditorContent.ShowBuildConfirmationDialog += OnShowBuildConfirmationDialog;
        
        // Initialize bottom panel tabs
        InitializeBottomPanelTabs();
    }

    public ObservableCollection<FileSystemItemViewModel> FileSystemItems { get; }
    
    public ObservableCollection<BottomPanelTabViewModel> BottomPanelTabs { get; }
    
    public EditorTabBarViewModel EditorTabBar { get; }
    
    public EditorContentViewModel EditorContent { get; }

    public string Title => "Project Documentation Manager";

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }


    public FileSystemItemViewModel? RootItem
    {
        get => _rootItem;
        private set => SetProperty(ref _rootItem, value);
    }

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
    
    public event EventHandler? ExitRequested;
    public event EventHandler<BuildConfirmationDialogViewModel>? ShowBuildConfirmationDialog;

    private async Task LoadFileStructureAsync()
    {
        try
        {
            IsLoading = true;
            _logger.LogInformation("Loading file structure...");
            
            var fileStructure = await _fileService.GetFileStructureAsync();
            
            if (fileStructure != null)
            {
                var rootViewModel = new FileSystemItemViewModel(fileStructure, isRoot: true, fileService: _fileService);
                RootItem = rootViewModel;
                
                FileSystemItems.Clear();
                FileSystemItems.Add(rootViewModel);
                
                // Start file system monitoring
                _fileService.StartFileSystemMonitoring();
                
                _logger.LogInformation("File structure loaded successfully");
            }
            else
            {
                _logger.LogWarning("Failed to load file structure");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading file structure");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task InitializeAsync()
    {
        await LoadFileStructureAsync();
    }
    
    public async Task RefreshFileStructureAsync()
    {
        await LoadFileStructureAsync();
    }

    private async void OnFileSelected(string filePath)
    {
        await EditorTabBar.OpenFileAsync(filePath);
    }

    public async Task SaveActiveFileAsync()
    {
        await EditorTabBar.SaveActiveFileAsync();
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
}
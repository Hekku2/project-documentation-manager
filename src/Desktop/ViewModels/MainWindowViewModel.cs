using System;
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

namespace Desktop.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly ApplicationOptions _applicationOptions;
    private readonly IFileService _fileService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMarkdownCombinationService _markdownCombinationService;
    private readonly IMarkdownFileCollectorService _markdownFileCollectorService;
    private bool _isLoading;
    private FileSystemItemViewModel? _rootItem;
    private EditorTabViewModel? _activeTab;
    private bool _isBottomPanelVisible = false;
    private BottomPanelTabViewModel? _activeBottomTab;
    private ValidationResult? _currentValidationResult;

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger, 
        IOptions<ApplicationOptions> applicationOptions, 
        IFileService fileService,
        IServiceProvider serviceProvider,
        IMarkdownCombinationService markdownCombinationService,
        IMarkdownFileCollectorService markdownFileCollectorService)
    {
        _logger = logger;
        _applicationOptions = applicationOptions.Value;
        _fileService = fileService;
        _serviceProvider = serviceProvider;
        _markdownCombinationService = markdownCombinationService;
        _markdownFileCollectorService = markdownFileCollectorService;
        FileSystemItems = new ObservableCollection<FileSystemItemViewModel>();
        EditorTabs = new ObservableCollection<EditorTabViewModel>();
        BottomPanelTabs = new ObservableCollection<BottomPanelTabViewModel>();
        ExitCommand = new RelayCommand(RequestApplicationExit);
        BuildDocumentationCommand = new RelayCommand(BuildDocumentation, CanBuildDocumentation);
        ValidateCommand = new RelayCommand(ValidateDocumentation, CanValidateDocumentation);
        ValidateAllCommand = new RelayCommand(ValidateAllDocumentation, CanValidateAllDocumentation);
        CloseBottomPanelCommand = new RelayCommand(CloseBottomPanel);
        ShowLogsCommand = new RelayCommand(ShowLogOutput);
        ShowErrorsCommand = new RelayCommand(ShowErrorOutput);
        
        _logger.LogInformation("MainWindowViewModel initialized");
        _logger.LogInformation("Default theme: {Theme}", _applicationOptions.DefaultTheme);
        _logger.LogInformation("Default project folder: {Folder}", _applicationOptions.DefaultProjectFolder);
        _logger.LogInformation("Default output folder: {OutputFolder}", _applicationOptions.DefaultOutputFolder);
        
        // Subscribe to file selection events
        FileSystemItemViewModel.FileSelected += OnFileSelected;
        
        // Initialize bottom panel tabs
        InitializeBottomPanelTabs();
        
        // Load file structure asynchronously
        _ = LoadFileStructureAsync();
    }

    public ObservableCollection<FileSystemItemViewModel> FileSystemItems { get; }
    
    public ObservableCollection<EditorTabViewModel> EditorTabs { get; }
    
    public ObservableCollection<BottomPanelTabViewModel> BottomPanelTabs { get; }

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

    public EditorTabViewModel? ActiveTab
    {
        get => _activeTab;
        private set => SetProperty(ref _activeTab, value);
    }

    public string? ActiveFileContent => ActiveTab?.Content;

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

    public ValidationResult? CurrentValidationResult
    {
        get => _currentValidationResult;
        private set => SetProperty(ref _currentValidationResult, value);
    }
    
    public ICommand ExitCommand { get; }
    
    public ICommand BuildDocumentationCommand { get; }
    
    public ICommand ValidateCommand { get; }
    
    public ICommand ValidateAllCommand { get; }
    
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

    public async Task RefreshFileStructureAsync()
    {
        await LoadFileStructureAsync();
    }

    private async void OnFileSelected(string filePath)
    {
        await OpenFileAsync(filePath);
    }

    public async Task OpenFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        // Check if tab already exists
        var existingTab = EditorTabs.FirstOrDefault(t => t.FilePath == filePath);
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
                FilePath = filePath,
                Content = content,
                IsModified = false,
                IsActive = true
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

    public void SetActiveTab(EditorTabViewModel tab)
    {
        // Deactivate current active tab
        if (ActiveTab != null)
        {
            ActiveTab.IsActive = false;
        }

        // Set new active tab
        tab.IsActive = true;
        ActiveTab = tab;
        OnPropertyChanged(nameof(ActiveFileContent));
        
        // Clear validation results when switching files
        CurrentValidationResult = null;
        
        // Update command states
        ((RelayCommand)ValidateCommand).RaiseCanExecuteChanged();
        
        _logger.LogDebug("Active tab changed to: {TabTitle}", tab.Title);
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
            ActiveTab = EditorTabs.FirstOrDefault();
            if (ActiveTab != null)
            {
                ActiveTab.IsActive = true;
            }
            OnPropertyChanged(nameof(ActiveFileContent));
            
            // Update command states when active tab changes
            ((RelayCommand)ValidateCommand).RaiseCanExecuteChanged();
        }
        
        _logger.LogDebug("Tab closed: {TabTitle}", tab.Title);
    }

    public async Task SaveActiveFileAsync()
    {
        if (ActiveTab == null || !ActiveTab.IsModified)
            return;

        try
        {
            var success = await _fileService.WriteFileContentAsync(ActiveTab.FilePath, ActiveTab.Content);
            if (success)
            {
                ActiveTab.IsModified = false;
                _logger.LogInformation("File saved: {FilePath}", ActiveTab.FilePath);
            }
            else
            {
                _logger.LogWarning("Failed to save file: {FilePath}", ActiveTab.FilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving file: {FilePath}", ActiveTab.FilePath);
        }
    }

    private void RequestApplicationExit()
    {
        _logger.LogInformation("Application exit requested");
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    private void BuildDocumentation()
    {
        _logger.LogInformation("Build documentation requested");
        
        var dialogViewModel = _serviceProvider.GetRequiredService<BuildConfirmationDialogViewModel>();
        ShowBuildConfirmationDialog?.Invoke(this, dialogViewModel);
    }

    private bool CanBuildDocumentation()
    {
        return true;
    }

    private async void ValidateDocumentation()
    {
        if (ActiveTab == null)
        {
            _logger.LogWarning("Validate requested but no active file");
            return;
        }
        
        _logger.LogInformation("Validating file: {FilePath}", ActiveTab.FilePath);

        try
        {
            // Get the directory containing the file to find source documents
            var fileDirectory = System.IO.Path.GetDirectoryName(ActiveTab.FilePath);
            if (string.IsNullOrEmpty(fileDirectory))
            {
                _logger.LogError("Could not determine directory for file: {FilePath}", ActiveTab.FilePath);
                return;
            }

            // Create MarkdownDocument for the active file
            var activeFileContent = ActiveTab.Content ?? string.Empty;
            var fileName = System.IO.Path.GetFileName(ActiveTab.FilePath);
            var templateDocument = new MarkdownDocument
            {
                FileName = fileName,
                Content = activeFileContent
            };

            // Collect source documents from the same directory
            var sourceDocuments = await _markdownFileCollectorService.CollectSourceFilesAsync(fileDirectory);
            
            // Validate the template document
            var validationResult = _markdownCombinationService.Validate(templateDocument, sourceDocuments);
            
            // Store validation results for UI highlighting
            CurrentValidationResult = validationResult;
            
            // Update error panel with validation results
            UpdateErrorPanelWithValidationResults(validationResult);
            
            // Log validation results
            if (validationResult.IsValid)
            {
                _logger.LogInformation("Validation successful for file: {FilePath}", ActiveTab.FilePath);
            }
            else
            {
                _logger.LogWarning("Validation failed for file: {FilePath}. Found {ErrorCount} errors and {WarningCount} warnings.", 
                    ActiveTab.FilePath, validationResult.Errors.Count, validationResult.Warnings.Count);
                    
                foreach (var error in validationResult.Errors)
                {
                    _logger.LogError("Validation error: {Message} at line {LineNumber}", error.Message, error.LineNumber);
                }
                
                foreach (var warning in validationResult.Warnings)
                {
                    _logger.LogWarning("Validation warning: {Message} at line {LineNumber}", warning.Message, warning.LineNumber);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during validation of file: {FilePath}", ActiveTab.FilePath);
        }
    }

    private bool CanValidateDocumentation()
    {
        return ActiveTab != null;
    }

    private async void ValidateAllDocumentation()
    {
        _logger.LogInformation("Validate all documentation requested");

        try
        {
            // Collect all template and source files from the project directory
            var (templateFiles, sourceFiles) = await _markdownFileCollectorService.CollectAllMarkdownFilesAsync(_applicationOptions.DefaultProjectFolder);
            
            if (!templateFiles.Any())
            {
                _logger.LogWarning("No template files found for validation");
                return;
            }

            _logger.LogInformation("Validating {TemplateCount} template files and {SourceCount} source files", 
                templateFiles.Count(), sourceFiles.Count());
            
            // Validate all templates
            var validationResult = _markdownCombinationService.ValidateAll(templateFiles, sourceFiles);
            
            // Store validation results for UI highlighting
            CurrentValidationResult = validationResult;
            
            // Update error panel with validation results
            UpdateErrorPanelWithValidationResults(validationResult);
            
            // Log validation results
            if (validationResult.IsValid)
            {
                _logger.LogInformation("Validation successful for all {TemplateCount} template files", templateFiles.Count());
            }
            else
            {
                _logger.LogWarning("Validation failed. Found {ErrorCount} errors and {WarningCount} warnings across all template files.", 
                    validationResult.Errors.Count, validationResult.Warnings.Count);
                    
                foreach (var error in validationResult.Errors)
                {
                    _logger.LogError("Validation error: {Message} at line {LineNumber}", error.Message, error.LineNumber);
                }
                
                foreach (var warning in validationResult.Warnings)
                {
                    _logger.LogWarning("Validation warning: {Message} at line {LineNumber}", warning.Message, warning.LineNumber);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during validation of all documentation files");
        }
    }

    private bool CanValidateAllDocumentation()
    {
        return true; // Can always validate all files
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
            Content = "",
            IsActive = false,
            IsClosable = true
        };
        var tabViewModel = new BottomPanelTabViewModel(tabModel);
        tabViewModel.CloseRequested += OnBottomTabCloseRequested;
        tabViewModel.SelectRequested += OnBottomTabSelectRequested;
        BottomPanelTabs.Add(tabViewModel);
        
        return tabViewModel;
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
            }
            
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
            }
            
            errorTab.Content = errorContent.ToString().TrimEnd();
            
            // Show the error panel only when there are errors or warnings
            SetActiveBottomTab(errorTab);
        }
        // If validation passes, don't create or show the error panel
    }
}
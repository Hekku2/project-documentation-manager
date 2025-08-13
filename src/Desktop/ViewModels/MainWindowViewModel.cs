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

namespace Desktop.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly ApplicationOptions _applicationOptions;
    private readonly IFileService _fileService;
    private bool _isLoading;
    private FileSystemItemViewModel? _rootItem;
    private EditorTabViewModel? _activeTab;

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger, 
        IOptions<ApplicationOptions> applicationOptions, 
        IFileService fileService)
    {
        _logger = logger;
        _applicationOptions = applicationOptions.Value;
        _fileService = fileService;
        FileSystemItems = new ObservableCollection<FileSystemItemViewModel>();
        EditorTabs = new ObservableCollection<EditorTabViewModel>();
        ExitCommand = new RelayCommand(RequestApplicationExit);
        
        _logger.LogInformation("MainWindowViewModel initialized");
        _logger.LogInformation("Default theme: {Theme}", _applicationOptions.DefaultTheme);
        _logger.LogInformation("Default project folder: {Folder}", _applicationOptions.DefaultProjectFolder);
        
        // Subscribe to file selection events
        FileSystemItemViewModel.FileSelected += OnFileSelected;
        
        // Load file structure asynchronously
        _ = LoadFileStructureAsync();
    }

    public ObservableCollection<FileSystemItemViewModel> FileSystemItems { get; }
    
    public ObservableCollection<EditorTabViewModel> EditorTabs { get; }

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
    
    public ICommand ExitCommand { get; }
    
    public event EventHandler? ExitRequested;

    private async Task LoadFileStructureAsync()
    {
        try
        {
            IsLoading = true;
            _logger.LogInformation("Loading file structure...");
            
            var fileStructure = await _fileService.GetFileStructureAsync();
            
            if (fileStructure != null)
            {
                var rootViewModel = new FileSystemItemViewModel(fileStructure, isRoot: true);
                RootItem = rootViewModel;
                
                FileSystemItems.Clear();
                FileSystemItems.Add(rootViewModel);
                
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
            _logger.LogInformation("Opening file: {FilePath}", filePath);
            
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
            
            _logger.LogInformation("File opened successfully: {FilePath}", filePath);
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
}
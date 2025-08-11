using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Desktop.Configuration;
using Desktop.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace Desktop.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly ApplicationOptions _applicationOptions;
    private readonly IFileService _fileService;
    private bool _isLoading;
    private FileSystemItemViewModel? _rootItem;

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger, 
        IOptions<ApplicationOptions> applicationOptions, 
        IFileService fileService)
    {
        _logger = logger;
        _applicationOptions = applicationOptions.Value;
        _fileService = fileService;
        FileSystemItems = new ObservableCollection<FileSystemItemViewModel>();
        
        _logger.LogInformation("MainWindowViewModel initialized");
        _logger.LogInformation("Default theme: {Theme}", _applicationOptions.DefaultTheme);
        _logger.LogInformation("Default project folder: {Folder}", _applicationOptions.DefaultProjectFolder);
        
        // Load file structure asynchronously
        _ = LoadFileStructureAsync();
    }

    public ObservableCollection<FileSystemItemViewModel> FileSystemItems { get; }

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

    private async Task LoadFileStructureAsync()
    {
        try
        {
            IsLoading = true;
            _logger.LogInformation("Loading file structure...");
            
            var fileStructure = await _fileService.GetFileStructureAsync();
            
            if (fileStructure != null)
            {
                var rootViewModel = new FileSystemItemViewModel(fileStructure);
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
}
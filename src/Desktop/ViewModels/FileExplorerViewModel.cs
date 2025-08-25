using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Desktop.Services;

namespace Desktop.ViewModels;

public class FileExplorerViewModel : ViewModelBase, IDisposable
{
    private readonly ILogger<FileExplorerViewModel> _logger;
    private readonly IFileService _fileService;
    private bool _isLoading;
    private FileSystemItemViewModel? _rootItem;

    public FileExplorerViewModel(ILogger<FileExplorerViewModel> logger, IFileService fileService)
    {
        _logger = logger;
        _fileService = fileService;
        FileSystemItems = [];
        
        _logger.LogInformation("FileExplorerViewModel initialized");
    }

    public ObservableCollection<FileSystemItemViewModel> FileSystemItems { get; }

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

    public event EventHandler<string>? FileSelected;

    public async Task InitializeAsync()
    {
        await LoadFileStructureAsync();
    }

    public async Task RefreshAsync()
    {
        await LoadFileStructureAsync();
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
                var rootViewModel = new FileSystemItemViewModel(
                    fileStructure,
                    isRoot: true,
                    fileService: _fileService,
                    onFileSelected: OnFileSelected // pass instance callback
                );
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

    private void OnFileSelected(string filePath)
    {
        _logger.LogInformation("File selected: {FilePath}", filePath);
        FileSelected?.Invoke(this, filePath);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private bool _disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            // No static event to unsubscribe
            _disposed = true;
        }
    }

    ~FileExplorerViewModel()
    {
        Dispose(false);
    }
}
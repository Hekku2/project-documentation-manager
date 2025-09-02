using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Desktop.Services;
using Desktop.Factories;

namespace Desktop.ViewModels;

public class FileExplorerViewModel(
    ILogger<FileExplorerViewModel> logger,
    ILoggerFactory loggerFactory,
    IFileSystemExplorerService fileSystemExplorerService,
    IFileService fileService) : ViewModelBase, IDisposable
{
    private bool _isLoading;
    private FileSystemItemViewModel? _rootItem;

    public ObservableCollection<FileSystemItemViewModel> FileSystemItems { get; } = [];

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
    public event EventHandler<string>? FilePreview;

    public async Task InitializeAsync()
    {
        logger.LogInformation("FileExplorerViewModel initialized");
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
            logger.LogInformation("Loading file structure...");
            
            var fileStructure = await fileService.GetFileStructureAsync();
            
            var factory = new FileSystemItemViewModelFactory(
                loggerFactory,
                fileService,
                fileSystemExplorerService,
                onItemSelected: OnFileSelected,
                onItemPreview: OnFilePreview);

            if (fileStructure != null)
            {
                var rootViewModel = factory.CreateRoot(
                    fileStructure
                );
                RootItem = rootViewModel;

                FileSystemItems.Clear();
                FileSystemItems.Add(rootViewModel);

                // Start file system monitoring
                fileService.StartFileSystemMonitoring();

                logger.LogInformation("File structure loaded successfully");
            }
            else
            {
                logger.LogWarning("Failed to load file structure");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading file structure");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnFileSelected(string filePath)
    {
        logger.LogInformation("File selected: {FilePath}", filePath);
        FileSelected?.Invoke(this, filePath);
    }

    private void OnFilePreview(string filePath)
    {
        logger.LogInformation("File preview requested: {FilePath}", filePath);
        FilePreview?.Invoke(this, filePath);
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
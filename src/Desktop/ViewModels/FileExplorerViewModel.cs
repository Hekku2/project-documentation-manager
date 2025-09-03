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
    IFileSystemChangeHandler fileSystemChangeHandler,
    IFileService fileService) : ViewModelBase, IDisposable
{
    private bool _isLoading;
    private FileSystemItemViewModel? _rootItem;
    private EventHandler<FileSystemChangedEventArgs>? _eventHandler;

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
                fileSystemExplorerService,
                onItemSelected: OnItemSelected,
                onItemPreview: OnItemPreview);

            if (fileStructure != null)
            {
                var rootViewModel = factory.CreateWithChildren(
                    fileStructure
                );
                RootItem = rootViewModel;

                FileSystemItems.Clear();
                FileSystemItems.Add(rootViewModel);

                // Start file system monitoring 
                _eventHandler = (sender, e) =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        fileSystemChangeHandler.HandleFileSystemChange(e, rootViewModel, factory);
                    });
                };

                fileService.FileSystemChanged += _eventHandler;

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

    private void OnItemSelected(string filePath)
    {
        logger.LogInformation("File selected: {FilePath}", filePath);
        FileSelected?.Invoke(this, filePath);
    }

    private void OnItemPreview(string filePath)
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
            if (_eventHandler != null && fileService != null)
            {
                fileService.FileSystemChanged -= _eventHandler;
            }
            fileService?.StopFileSystemMonitoring();

            _disposed = true;
        }
    }

    ~FileExplorerViewModel()
    {
        Dispose(false);
    }
}
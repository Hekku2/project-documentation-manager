using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Desktop.Services;
using Desktop.Factories;

namespace Desktop.ViewModels;

public class FileExplorerViewModel : ViewModelBase, IDisposable
{
    private bool _isLoading;
    private FileSystemItemViewModel? _rootItem;
    private bool _disposed = false;
    private readonly ILogger<FileExplorerViewModel> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IFileSystemExplorerService _fileSystemExplorerService;
    private readonly IFileSystemChangeHandler _fileSystemChangeHandler;
    private readonly IFileService _fileService;
    private readonly FileSystemItemViewModelFactory _fileSystemItemViewModelFactory;

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

    public FileExplorerViewModel(
        ILogger<FileExplorerViewModel> logger,
        ILoggerFactory loggerFactory,
        IFileSystemExplorerService fileSystemExplorerService,
        IFileSystemChangeHandler fileSystemChangeHandler,
        IFileService fileService)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _fileSystemExplorerService = fileSystemExplorerService;
        _fileSystemChangeHandler = fileSystemChangeHandler;
        _fileService = fileService;

        _fileSystemItemViewModelFactory = new FileSystemItemViewModelFactory(
            _loggerFactory,
            _fileSystemExplorerService,
            onItemSelected: OnItemSelected,
            onItemPreview: OnItemPreview);

        _fileService.FileSystemChanged += DelegateFileSystemChangeEvent;
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("FileExplorerViewModel initialized");
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
                var rootViewModel = _fileSystemItemViewModelFactory.CreateWithChildren(
                    fileStructure
                );

                RootItem = rootViewModel;

                FileSystemItems.Clear();
                FileSystemItems.Add(rootViewModel);

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

    private void DelegateFileSystemChangeEvent(object? sender, FileSystemChangedEventArgs eventArgs)
    {
        var root = _rootItem;
        if (root is null)
            return;

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _fileSystemChangeHandler.HandleFileSystemChange(eventArgs, root, _fileSystemItemViewModelFactory);
        });
    }

    private void OnItemSelected(string filePath)
    {
        _logger.LogInformation("File selected: {FilePath}", filePath);
        FileSelected?.Invoke(this, filePath);
    }

    private void OnItemPreview(string filePath)
    {
        _logger.LogInformation("File preview requested: {FilePath}", filePath);
        FilePreview?.Invoke(this, filePath);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _fileService.FileSystemChanged -= DelegateFileSystemChangeEvent;
            _fileService.StopFileSystemMonitoring();

            _disposed = true;
        }
    }

    ~FileExplorerViewModel()
    {
        Dispose(false);
    }
}
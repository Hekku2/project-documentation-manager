using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Desktop.Services;
using Desktop.Factories;
using Desktop.Configuration;

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
    private readonly IFileSystemMonitorService _fileSystemMonitor;
    private readonly ApplicationOptions _applicationOptions;
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
        IFileService fileService,
        IFileSystemMonitorService fileSystemMonitor,
        IOptions<ApplicationOptions> applicationOptions)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _fileSystemExplorerService = fileSystemExplorerService;
        _fileSystemChangeHandler = fileSystemChangeHandler;
        _fileService = fileService;
        _fileSystemMonitor = fileSystemMonitor;
        _applicationOptions = applicationOptions.Value;

        _fileSystemItemViewModelFactory = new FileSystemItemViewModelFactory(
            _loggerFactory,
            _fileSystemExplorerService,
            onItemSelected: OnItemSelected,
            onItemPreview: OnItemPreview);

        _fileSystemMonitor.FileSystemChanged += DelegateFileSystemChangeEvent;
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

                if (!string.IsNullOrEmpty(_applicationOptions.DefaultProjectFolder))
                {
                    _fileSystemMonitor.StartMonitoring(_applicationOptions.DefaultProjectFolder);
                }

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
            _fileSystemMonitor.FileSystemChanged -= DelegateFileSystemChangeEvent;
            _fileSystemMonitor.StopMonitoring();
            _fileSystemMonitor.Dispose();

            _disposed = true;
        }
    }

    ~FileExplorerViewModel()
    {
        Dispose(false);
    }
}
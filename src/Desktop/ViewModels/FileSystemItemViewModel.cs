using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Desktop.Models;
using Desktop.Services;
using Desktop.Factories;
using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Desktop.ViewModels;

public class FileSystemItemViewModel : ViewModelBase
{
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    private bool _isExpanded;
    private bool _isSelected;
    private bool _isLoading;
    private bool _childrenLoaded;
    private bool _isVisible;

    private readonly Action<string> _onItemSelected;
    private readonly Action<string> _onItemPreview;

    private readonly ILogger<FileSystemItemViewModel> _logger;
    private readonly IFileSystemExplorerService _fileSystemExplorerService;
    private readonly IFileSystemItemViewModelFactory _viewModelFactory;

    public FileSystemItemViewModel(
        ILogger<FileSystemItemViewModel> logger,
        IFileSystemItemViewModelFactory viewModelFactory,
        IFileSystemExplorerService fileSystemExplorerService,
        FileSystemItem item,
        bool loadChildren,
        Action<string> onItemSelected,
        Action<string> onItemPreview
        )
    {
        _logger = logger;
        Item = item;
        Children = [];
        _onItemSelected = onItemSelected;
        _onItemPreview = onItemPreview;
        _fileSystemExplorerService = fileSystemExplorerService;
        _viewModelFactory = viewModelFactory;

        // Initialize context menu commands
        OpenCommand = new RelayCommand(ExecuteOpen, CanExecuteOpen);
        NewCommand = new RelayCommand(() => { }, () => false); // Disabled command for directories
        ShowInExplorerCommand = new RelayCommand(ExecuteShowInExplorer, CanExecuteShowInExplorer);
        CopyPathCommand = new RelayCommand(ExecuteCopyPath, CanExecutePathCommand);
        RefreshCommand = new RelayCommand(ExecuteRefresh, CanExecuteRefresh);
        ShowInPreviewCommand = new RelayCommand(ExecuteShowInPreview, CanExecuteShowInPreview);

        // For files, mark as loaded since they don't have children
        if (!item.IsDirectory)
        {
            // For files, mark as loaded since they don't have children
            _childrenLoaded = true;
        }

        // Auto-expand root directory for better UX
        if (item.IsDirectory && loadChildren)
        {
            // Mark as visible and load children immediately for root
            _isVisible = true;
            _ = Task.Run(async () =>
            {
                await LoadChildrenAsync(false); // No deep preloading for root
                // Set expanded after loading to trigger UI update
                Avalonia.Threading.Dispatcher.UIThread.Post(() => IsExpanded = true);
            });
        }
    }

    public FileSystemItem Item { get; }

    public ObservableCollection<FileSystemItemViewModel> Children { get; }

    public string Name => Item.DisplayName;
    public string FullPath => Item.FullPath;
    public bool IsDirectory => Item.IsDirectory;
    public bool HasChildren => IsDirectory && (!_childrenLoaded || Children.Count > 0);
    public bool HasChildrenLoaded => _childrenLoaded;
    public bool IsMarkdownTemplate => !IsDirectory && (FullPath.EndsWith(".mdext", StringComparison.OrdinalIgnoreCase) ||
                                                         FullPath.EndsWith(".mdsrc", StringComparison.OrdinalIgnoreCase) ||
                                                         FullPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase));

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (SetProperty(ref _isExpanded, value))
            {
                if (value && !_childrenLoaded)
                {
                    _ = LoadChildrenAsync(true); // Enable deep preloading for manual expansion
                }
                else if (value && _childrenLoaded)
                {
                    // Already loaded but now expanded - load children of child folders
                    PreloadNextLevel();
                }
            }
        }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (SetProperty(ref _isVisible, value) && value && !_childrenLoaded && IsDirectory)
            {
                // Folder became visible - load its direct children
                _ = LoadChildrenAsync(false); // Load children but don't preload deeper levels yet
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value) && value && !IsDirectory)
            {
                // File was selected, notify via callback
                _onItemSelected.Invoke(FullPath);
            }
        }
    }

    // Context Menu Commands
    public ICommand OpenCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand ShowInExplorerCommand { get; }
    public ICommand CopyPathCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ShowInPreviewCommand { get; }


    private bool CanExecuteOpen() => !string.IsNullOrEmpty(FullPath);
    private bool CanExecuteShowInExplorer() => !string.IsNullOrEmpty(FullPath) && (File.Exists(FullPath) || Directory.Exists(FullPath));
    private bool CanExecutePathCommand() => !string.IsNullOrEmpty(FullPath);
    private bool CanExecuteRefresh() => IsDirectory && !string.IsNullOrEmpty(FullPath);
    private bool CanExecuteShowInPreview() => IsMarkdownTemplate;

    private void ExecuteOpen()
    {
        if (IsDirectory)
        {
            // For directories, just expand them
            IsExpanded = !IsExpanded;
        }
        else
        {
            // For files, open them in the editor
            _onItemSelected.Invoke(FullPath);
        }
    }


    private void ExecuteShowInExplorer()
    {
        _fileSystemExplorerService.ShowInExplorer(FullPath);
    }

    private async void ExecuteCopyPath()
    {
        try
        {
            if (!string.IsNullOrEmpty(FullPath))
            {
                // Access clipboard through TopLevel
                var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null);
                if (topLevel?.Clipboard != null)
                {
                    await topLevel.Clipboard.SetTextAsync(FullPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy path to clipboard: {Path}", FullPath);
        }
    }

    private void ExecuteRefresh()
    {
        if (IsDirectory && _childrenLoaded)
        {
            // Clear children and reload
            _childrenLoaded = false;
            Children.Clear();

            if (IsVisible)
            {
                // If folder is visible, reload with appropriate preloading based on expansion state
                _ = LoadChildrenAsync(IsExpanded); // Enable deep preloading if expanded
            }
        }
    }

    private void ExecuteShowInPreview()
    {
        // Open the file in preview mode using the callback
        _onItemPreview.Invoke(FullPath);
    }

    private async Task LoadChildrenAsync(bool enableDeepPreloading = false)
    {
        _logger.LogTrace("Loading children for {Path}, DeepPreloading: {DeepPreloading}", FullPath, enableDeepPreloading);

        if (_childrenLoaded || IsLoading || !Item.IsDirectory)
            return;

        try
        {
            IsLoading = true;

            // Use shared core logic for creating and updating children
            var childViewModels = await CreateSortedChildViewModelsAsync();
            await UpdateUIWithChildrenAsync(childViewModels);

            _childrenLoaded = true;

            // Mark all child folders as visible since they are now in the TreeView
            foreach (var child in childViewModels.Where(c => c.IsDirectory))
            {
                child.IsVisible = true;
            }

            // Only preload the next level if explicitly enabled (when folder is expanded)
            if (enableDeepPreloading)
            {
                PreloadNextLevel();
            }
        }
        finally
        {
            IsLoading = false;
            _logger.LogTrace("Finished loading children for {Path}", FullPath);
        }
    }

    private async void PreloadNextLevel()
    {
        // Get all directory children that aren't already loaded
        var directoryChildren = Children
            .Where(child => child.IsDirectory && !child.HasChildrenLoaded)
            .ToList();

        if (directoryChildren.Count == 0)
            return;

        // Start preloading tasks for each directory child
        var preloadTasks = directoryChildren.Select(async child =>
        {
            try
            {
                // Load children without expanding the UI
                await child.LoadChildrenInternalAsync();
            }
            catch
            {
                // Ignore preloading errors - they shouldn't affect the main functionality
            }
        });

        // Wait for all preloading to complete in the background
        await Task.WhenAll(preloadTasks);
    }

    private async Task LoadChildrenInternalAsync()
    {
        if (_childrenLoaded || IsLoading || !Item.IsDirectory)
            return;

        try
        {
            // Use shared core logic for creating and updating children (for preloading)
            var childViewModels = await CreateSortedChildViewModelsAsync();
            await UpdateUIWithChildrenAsync(childViewModels);

            _childrenLoaded = true;

            // Mark all child folders as visible since they are now in the TreeView
            foreach (var child in childViewModels.Where(c => c.IsDirectory))
            {
                child.IsVisible = true;
            }
        }
        catch
        {
            // Handle any loading errors silently for preloading
        }
    }

    private async Task<FileSystemItemViewModel[]> CreateSortedChildViewModelsAsync()
    {
        return await Task.Run(() =>
        {
            // Single-pass sorting using custom comparer for better performance
            var sortedChildren = Item.Children.ToArray();
            Array.Sort(sortedChildren, (x, y) =>
            {
                // Directories first, then files
                var directoryComparison = y.IsDirectory.CompareTo(x.IsDirectory);
                if (directoryComparison != 0) return directoryComparison;

                // Then alphabetical by name
                return string.Compare(x.Name, y.Name, PathComparison);
            });

            return sortedChildren
                .Select(child => _viewModelFactory.Create(child))
                .ToArray();
        });
    }

    private async Task UpdateUIWithChildrenAsync(FileSystemItemViewModel[] childViewModels)
    {
        // Batch update UI on the main thread for better performance
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            Children.Clear();

            // Batch add all children at once to reduce UI notification overhead
            foreach (var childViewModel in childViewModels)
            {
                Children.Add(childViewModel);
            }
        });
    }
}
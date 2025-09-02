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
    private bool _isExpanded;
    private bool _isSelected;
    private bool _isLoading;
    private bool _childrenLoaded;
    private bool _isVisible;

    // Instance-level file selected callback
    private readonly Action<string>? _onFileSelected;
    private readonly Action<string>? _onFilePreview;

    private readonly ILogger<FileSystemItemViewModel> _logger;
    private readonly IFileService? _fileService;
    private readonly IFileSystemExplorerService _fileSystemExplorerService;
    private readonly FileSystemItemViewModelFactory _viewModelFactory;

    public FileSystemItemViewModel(
        ILogger<FileSystemItemViewModel> logger,
        FileSystemItemViewModelFactory viewModelFactory,
        IFileSystemExplorerService fileSystemExplorerService,
        FileSystemItem item,
        bool isRoot = false,
        IFileService? fileService = null,
        Action<string>? onFileSelected = null,
        Action<string>? onFilePreview = null
        )
    {
        _logger = logger;
        Item = item;
        Children = [];
        _fileService = fileService;
        _onFileSelected = onFileSelected;
        _onFilePreview = onFilePreview;
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
        
        // Subscribe to file system changes if we have a file service and this is the root
        if (isRoot && _fileService != null)
        {
            _fileService.FileSystemChanged += OnFileSystemChanged;
        }
        
        // Auto-expand root directory for better UX
        if (item.IsDirectory && isRoot)
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
                _onFileSelected?.Invoke(FullPath);
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
            _onFileSelected?.Invoke(FullPath);
        }
    }


    private void ExecuteShowInExplorer()
    {
        _fileSystemExplorerService?.ShowInExplorer(FullPath);
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
        catch
        {
            // Ignore if cannot access clipboard
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
        _onFilePreview?.Invoke(FullPath);
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
            .Where(child => child.IsDirectory && !child._childrenLoaded)
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
                return string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
            });
            
            return sortedChildren
                .Select(child => _viewModelFactory.Create(child, false, _onFileSelected, _onFilePreview))
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

    private void OnFileSystemChanged(object? sender, FileSystemChangedEventArgs e)
    {
        // Handle file system changes on the UI thread
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            HandleFileSystemChange(e);
        });
    }

    private void HandleFileSystemChange(FileSystemChangedEventArgs e)
    {
        // Find the parent folder that should contain this item
        var parentViewModel = FindParentForPath(e.Path);
        if (parentViewModel == null)
            return;

        switch (e.ChangeType)
        {
            case FileSystemChangeType.Created:
                HandleItemCreated(parentViewModel, e.Path, e.IsDirectory);
                break;
            case FileSystemChangeType.Deleted:
                HandleItemDeleted(parentViewModel, e.Path);
                break;
            case FileSystemChangeType.Renamed:
                if (!string.IsNullOrEmpty(e.OldPath))
                {
                    HandleItemDeleted(parentViewModel, e.OldPath);
                    HandleItemCreated(parentViewModel, e.Path, e.IsDirectory);
                }
                break;
            case FileSystemChangeType.Changed:
                HandleItemChanged(e.Path);
                break;
        }
    }

    private FileSystemItemViewModel? FindParentForPath(string filePath)
    {
        var parentPath = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(parentPath))
            return null;

        return FindViewModelByPath(parentPath);
    }

    private FileSystemItemViewModel? FindViewModelByPath(string path)
    {
        if (string.Equals(Item.FullPath, path, StringComparison.OrdinalIgnoreCase))
            return this;

        // Check if the requested path is a child of this item
        if (!path.StartsWith(Item.FullPath, StringComparison.OrdinalIgnoreCase))
            return null;

        // If children are loaded, search through them
        if (_childrenLoaded)
        {
            foreach (var child in Children)
            {
                var result = child.FindViewModelByPath(path);
                if (result != null)
                    return result;
            }
        }

        // If this is a directory and the path could be a child, return this as potential parent
        if (Item.IsDirectory && path.StartsWith(Item.FullPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return this; // Return this directory as the closest parent found
        }

        return null;
    }

    private void HandleItemCreated(FileSystemItemViewModel parentViewModel, string itemPath, bool isDirectory)
    {
        // Always update the underlying data structure first
        if (parentViewModel.Item.IsDirectory)
        {
            var childFileName = Path.GetFileName(itemPath);
            var existingItem = parentViewModel.Item.Children.FirstOrDefault(c => 
                string.Equals(c.Name, childFileName, StringComparison.OrdinalIgnoreCase));
            
            if (existingItem == null)
            {
                var childItem = _fileService?.CreateFileSystemItem(itemPath, isDirectory);
                if (childItem != null)
                {
                    parentViewModel.Item.Children.Add(childItem);
                }
            }
        }

        // If parent's UI children are not loaded, no need to update UI immediately
        if (!parentViewModel._childrenLoaded)
        {
            return;
        }

        // Check if item already exists
        var fileName = Path.GetFileName(itemPath);
        if (parentViewModel.Children.Any(c => string.Equals(c.Name, fileName, StringComparison.OrdinalIgnoreCase)))
            return;

        // Create the new file system item
        var newItem = _fileService?.CreateFileSystemItem(itemPath, isDirectory);
        if (newItem == null)
            return;

        var newViewModel = _viewModelFactory.Create(newItem, false, _onFileSelected, _onFilePreview);
        
        // Mark the new item as visible since its parent is expanded and loaded
        if (newItem.IsDirectory)
        {
            newViewModel.IsVisible = true;
        }

        // Find the correct position to insert (directories first, then alphabetical)
        var insertIndex = 0;
        for (var i = 0; i < parentViewModel.Children.Count; i++)
        {
            var existingChild = parentViewModel.Children[i];
            
            // Directories come before files
            if (isDirectory && !existingChild.IsDirectory)
                break;
            if (!isDirectory && existingChild.IsDirectory)
            {
                insertIndex = i + 1;
                continue;
            }

            // Within the same type, sort alphabetically
            if (string.Compare(fileName, existingChild.Name, StringComparison.OrdinalIgnoreCase) < 0)
                break;

            insertIndex = i + 1;
        }

        parentViewModel.Children.Insert(insertIndex, newViewModel);
    }

    private static void HandleItemDeleted(FileSystemItemViewModel parentViewModel, string itemPath)
    {
        var fileName = Path.GetFileName(itemPath);
        
        // Remove from UI children if loaded
        if (parentViewModel._childrenLoaded)
        {
            var itemToRemove = parentViewModel.Children.FirstOrDefault(c => 
                string.Equals(c.Name, fileName, StringComparison.OrdinalIgnoreCase));
            
            if (itemToRemove != null)
            {
                parentViewModel.Children.Remove(itemToRemove);
            }
        }
        
        // Also remove from underlying data structure
        if (parentViewModel.Item.IsDirectory)
        {
            var dataItemToRemove = parentViewModel.Item.Children.FirstOrDefault(c => 
                string.Equals(c.Name, fileName, StringComparison.OrdinalIgnoreCase));
            
            if (dataItemToRemove != null)
            {
                parentViewModel.Item.Children.Remove(dataItemToRemove);
            }
        }
    }

    private void HandleItemChanged(string itemPath)
    {
        var itemViewModel = FindViewModelByPath(itemPath);
        if (itemViewModel != null && !itemViewModel.IsDirectory)
        {
            // Update last modified time if needed
            try
            {
                var lastModified = File.GetLastWriteTime(itemPath);
                if (itemViewModel.Item.LastModified != lastModified)
                {
                    itemViewModel.Item.LastModified = lastModified;
                    itemViewModel.Item.Size = new FileInfo(itemPath).Length;
                    // Trigger property change notifications if needed
                }
            }
            catch
            {
                // Ignore errors when updating file info
            }
        }
    }


}
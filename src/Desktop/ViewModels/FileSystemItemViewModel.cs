using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Desktop.Models;
using Desktop.Services;
using System;
using System.IO;

namespace Desktop.ViewModels;

public class FileSystemItemViewModel : ViewModelBase
{
    private bool _isExpanded;
    private bool _isSelected;
    private bool _isLoading;
    private bool _childrenLoaded;

    // Instance-level file selected callback
    private readonly Action<string>? _onFileSelected;
    private readonly Action<string>? _onFilePreview;

    private readonly IFileService? _fileService;
    private readonly IFileSystemExplorerService _fileSystemExplorerService;

    public FileSystemItemViewModel(
        FileSystemItem item,
        IFileSystemExplorerService fileSystemExplorerService,
        bool isRoot = false,
        IFileService? fileService = null,
        Action<string>? onFileSelected = null,
        Action<string>? onFilePreview = null
        )
    {
        Item = item;
        Children = [];
        _fileService = fileService;
        _onFileSelected = onFileSelected;
        _onFilePreview = onFilePreview;
        _fileSystemExplorerService = fileSystemExplorerService;
        
        // Initialize context menu commands
        InitializeCommands();
        
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
        
        // Auto-expand only the actual root directory (without preloading)
        if (item.IsDirectory && isRoot)
        {
            _ = LoadChildrenAsync(true); // Don't preload for auto-expansion
            IsExpanded = true;
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
            if (SetProperty(ref _isExpanded, value) && value && !_childrenLoaded)
            {
                _ = LoadChildrenAsync(true); // Enable preloading for manual expansion
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
    public ICommand OpenCommand { get; private set; } = null!;
    public ICommand NewCommand { get; private set; } = null!;
    public ICommand ShowInExplorerCommand { get; private set; } = null!;
    public ICommand CopyPathCommand { get; private set; } = null!;
    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand ShowInPreviewCommand { get; private set; } = null!;

    private void InitializeCommands()
    {
        OpenCommand = new RelayCommand(ExecuteOpen, CanExecuteOpen);
        NewCommand = new RelayCommand(() => { }, () => false); // Disabled command for directories
        ShowInExplorerCommand = new RelayCommand(ExecuteShowInExplorer, CanExecuteShowInExplorer);
        CopyPathCommand = new RelayCommand(ExecuteCopyPath, CanExecutePathCommand);
        RefreshCommand = new RelayCommand(ExecuteRefresh, CanExecuteRefresh);
        ShowInPreviewCommand = new RelayCommand(ExecuteShowInPreview, CanExecuteShowInPreview);
    }

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
            
            
            if (IsExpanded)
            {
                _ = LoadChildrenAsync(true); // Enable preloading on refresh since it's a manual action
            }
        }
    }

    private void ExecuteShowInPreview()
    {
        // Open the file in preview mode using the callback
        _onFilePreview?.Invoke(FullPath);
    }

    private async Task LoadChildrenAsync(bool enablePreloading = false)
    {
        if (_childrenLoaded || IsLoading || !Item.IsDirectory)
            return;

        try
        {
            IsLoading = true;
            
            await Task.Run(() =>
            {
                var childViewModels = Item.Children
                    .OrderBy(c => !c.IsDirectory)
                    .ThenBy(c => c.Name)
                    .Select(child => new FileSystemItemViewModel(child, _fileSystemExplorerService, false, _fileService, _onFileSelected, _onFilePreview))
                    .ToList();

                // Update the UI on the main thread
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    Children.Clear();
                    foreach (var childViewModel in childViewModels)
                    {
                        Children.Add(childViewModel);
                    }
                    
                    // Only preload the next level if explicitly enabled
                    if (enablePreloading)
                    {
                        PreloadNextLevel();
                    }
                });
            });

            _childrenLoaded = true;
        }
        finally
        {
            IsLoading = false;
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
            await Task.Run(() =>
            {
                var childViewModels = Item.Children
                    .OrderBy(c => !c.IsDirectory)
                    .ThenBy(c => c.Name)
                    .Select(child => new FileSystemItemViewModel(child, _fileSystemExplorerService, false, _fileService, _onFileSelected, _onFilePreview))
                    .ToList();

                // Update the UI on the main thread
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    Children.Clear();
                    foreach (var childViewModel in childViewModels)
                    {
                        Children.Add(childViewModel);
                    }
                });
            });

            _childrenLoaded = true;
        }
        catch
        {
            // Handle any loading errors silently for preloading
        }
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
        // If parent is not expanded or loaded, we still need to update the underlying data structure
        // but we don't need to update the UI immediately
        if (!parentViewModel.IsExpanded || !parentViewModel._childrenLoaded)
        {
            // Update the underlying Item.Children collection so the change is reflected when expanded
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

        var newViewModel = new FileSystemItemViewModel(newItem, _fileSystemExplorerService, false, _fileService, _onFileSelected, _onFilePreview);

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
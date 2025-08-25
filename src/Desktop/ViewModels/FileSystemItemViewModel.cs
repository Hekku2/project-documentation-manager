using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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

    private readonly IFileService? _fileService;

    public FileSystemItemViewModel(FileSystemItem item, bool isRoot = false, IFileService? fileService = null, Action<string>? onFileSelected = null)
    {
        Item = item;
        Children = [];
        _fileService = fileService;
        _onFileSelected = onFileSelected;
        
        // For directories, add a placeholder to show the expand icon
        if (item.IsDirectory && item.HasChildren)
        {
            Children.Add(new FileSystemItemViewModel(new FileSystemItem { Name = "Loading...", FullPath = "" }, false, null, _onFileSelected));
        }
        else if (!item.IsDirectory)
        {
            // For files, mark as loaded since they don't have children
            _childrenLoaded = true;
        }
        
        // Subscribe to file system changes if we have a file service and this is the root
        if (isRoot && _fileService != null)
        {
            _fileService.FileSystemChanged += OnFileSystemChanged;
        }
        
        // Auto-expand only the actual root directory
        if (item.IsDirectory && isRoot)
        {
            _ = LoadChildrenAsync();
            IsExpanded = true;
        }
    }

    public FileSystemItem Item { get; }
    
    public ObservableCollection<FileSystemItemViewModel> Children { get; }

    public string Name => Item.DisplayName;
    public string FullPath => Item.FullPath;
    public bool IsDirectory => Item.IsDirectory;
    public bool HasChildren => Children.Count > 0;

    public bool IsExpanded
    {
        get => _isExpanded;
        set 
        {
            if (SetProperty(ref _isExpanded, value) && value && !_childrenLoaded)
            {
                _ = LoadChildrenAsync();
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

    private async Task LoadChildrenAsync()
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
                    .Select(child => new FileSystemItemViewModel(child, false, _fileService, _onFileSelected))
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
        finally
        {
            IsLoading = false;
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

        if (!_childrenLoaded || !IsExpanded)
            return null;

        foreach (var child in Children)
        {
            var result = child.FindViewModelByPath(path);
            if (result != null)
                return result;
        }

        return null;
    }

    private void HandleItemCreated(FileSystemItemViewModel parentViewModel, string itemPath, bool isDirectory)
    {
        // Only handle if the parent is expanded and loaded
        if (!parentViewModel.IsExpanded || !parentViewModel._childrenLoaded)
            return;

        // Check if item already exists
        var fileName = Path.GetFileName(itemPath);
        if (parentViewModel.Children.Any(c => string.Equals(c.Name, fileName, StringComparison.OrdinalIgnoreCase)))
            return;

        // Create the new file system item
        var newItem = new FileSystemItem
        {
            Name = fileName,
            FullPath = itemPath,
            IsDirectory = isDirectory
        };

        // Try to get file/directory properties, but don't fail if they're not accessible
        try
        {
            newItem.LastModified = isDirectory ? Directory.GetLastWriteTime(itemPath) : File.GetLastWriteTime(itemPath);
            newItem.Size = isDirectory ? 0 : new FileInfo(itemPath).Length;
        }
        catch
        {
            // Use default values if file system access fails (e.g., during testing)
            newItem.LastModified = DateTime.Now;
            newItem.Size = 0;
        }

        // If it's a directory, populate its children for the lazy loading
        if (isDirectory)
        {
            try
            {
                var dirInfo = new DirectoryInfo(itemPath);
                var hasChildren = dirInfo.GetDirectories().Any(d => !IsHiddenOrSystem(d.Attributes)) ||
                                 dirInfo.GetFiles().Any(f => !IsHiddenOrSystem(f.Attributes));
                
                if (hasChildren)
                {
                    // Add a placeholder child to indicate it has children
                    newItem.Children.Add(new FileSystemItem { Name = "Placeholder", FullPath = "" });
                }
            }
            catch
            {
                // Ignore errors when checking for children
            }
        }

    var newViewModel = new FileSystemItemViewModel(newItem, false, _fileService, _onFileSelected);

        // Find the correct position to insert (directories first, then alphabetical)
        var insertIndex = 0;
        for (int i = 0; i < parentViewModel.Children.Count; i++)
        {
            var existingChild = parentViewModel.Children[i];
            
            // Skip the loading placeholder
            if (existingChild.Name == "Loading...")
                continue;

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

    private void HandleItemDeleted(FileSystemItemViewModel parentViewModel, string itemPath)
    {
        var fileName = Path.GetFileName(itemPath);
        var itemToRemove = parentViewModel.Children.FirstOrDefault(c => 
            string.Equals(c.Name, fileName, StringComparison.OrdinalIgnoreCase));
        
        if (itemToRemove != null)
        {
            parentViewModel.Children.Remove(itemToRemove);
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

    private static bool IsHiddenOrSystem(FileAttributes attributes)
    {
        return (attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0;
    }

}
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Desktop.Models;
using System;

namespace Desktop.ViewModels;

public class FileSystemItemViewModel : ViewModelBase
{
    private bool _isExpanded;
    private bool _isSelected;
    private bool _isLoading;
    private bool _childrenLoaded;

    public static event Action<string>? FileSelected;

    public FileSystemItemViewModel(FileSystemItem item, bool isRoot = false)
    {
        Item = item;
        Children = new ObservableCollection<FileSystemItemViewModel>();
        
        // For directories, add a placeholder to show the expand icon
        if (item.IsDirectory && item.HasChildren)
        {
            Children.Add(new FileSystemItemViewModel(new FileSystemItem { Name = "Loading..." }));
        }
        else if (!item.IsDirectory)
        {
            // For files, mark as loaded since they don't have children
            _childrenLoaded = true;
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
                // File was selected, notify listeners
                FileSelected?.Invoke(FullPath);
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
                    .Select(child => new FileSystemItemViewModel(child))
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

}